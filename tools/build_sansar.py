#!/usr/bin/env python3
"""
Universal Sansar Build Script v2.0
Merges multi-file projects into single .cs files for Sansar upload

Usage:
  python build_sansar.py                    # Auto-discover all build configs
  python build_sansar.py config.json        # Build specific config
  python build_sansar.py FolderName/        # Build folder with smart detection
  python build_sansar.py --all              # Build all discovered configs
"""

import json
import os
import re
import sys
import glob
from pathlib import Path
from typing import Dict, List, Optional, Any, Tuple

# Windows consoles often default to a legacy codepage that cannot print the
# status glyphs below; degrade to '?' instead of crashing.
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, 'reconfigure'):
        _stream.reconfigure(errors='replace')

class SansarBuilder:
    def __init__(self, config_file: str):
        self.config_file = config_file
        # All relative paths in the config (sources and output) are resolved
        # against the config file's directory, not the process cwd.
        self.config_dir = str(Path(config_file).resolve().parent)
        self.config = self.load_config()
        self.output_lines = []

    def resolve_path(self, path: str) -> str:
        if os.path.isabs(path):
            return path
        return os.path.join(self.config_dir, path)
        
    def load_config(self) -> Dict[str, Any]:
        """Load the project configuration"""
        if not os.path.exists(self.config_file):
            print(f"Error: Configuration file '{self.config_file}' not found!")
            sys.exit(1)
        
        with open(self.config_file, 'r') as f:
            return json.load(f)
    
    def extract_content(self, file_path: str, extract_mode: str) -> List[str]:
        """Extract content from a file based on the extraction mode"""
        if not os.path.exists(file_path):
            return []
        
        with open(file_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        
        if extract_mode == "content_only":
            # Extract content between namespace declaration
            in_namespace = False
            content = []
            brace_count = 0
            
            for line in lines:
                if 'namespace' in line and '{' in line:
                    in_namespace = True
                    brace_count = 1
                    continue
                elif in_namespace:
                    if '{' in line:
                        brace_count += line.count('{')
                    if '}' in line:
                        brace_count -= line.count('}')
                    
                    if brace_count > 0:
                        content.append(line.rstrip())
                    elif brace_count == 0:
                        break
            
            # Remove the last closing brace if present
            while content and content[-1].strip() in ['', '}']:
                content.pop()
            
            return content
        
        elif extract_mode == "class_content":
            # Extract class definition without namespace
            in_class = False
            content = []
            brace_count = 0
            
            for line in lines:
                if re.match(r'\s*public\s+(class|struct|enum)\s+\w+', line):
                    in_class = True
                    content.append(line.rstrip())
                    if '{' in line:
                        brace_count = 1
                    continue
                elif in_class:
                    content.append(line.rstrip())
                    if '{' in line:
                        brace_count += line.count('{')
                    if '}' in line:
                        brace_count -= line.count('}')
                    
                    if brace_count == 0:
                        break
            
            return content
        
        elif extract_mode == "partial_content":
            # Extract content of partial class without class declaration
            in_class = False
            content = []
            brace_count = 0
            skip_first_brace = True
            
            for line in lines:
                if 'partial class' in line:
                    in_class = True
                    if '{' in line:
                        brace_count = 1
                    continue
                elif in_class:
                    if skip_first_brace and line.strip() == '{':
                        skip_first_brace = False
                        brace_count = 1
                        continue
                    
                    if '{' in line:
                        brace_count += line.count('{')
                    if '}' in line:
                        brace_count -= line.count('}')
                    
                    if brace_count > 0:
                        content.append(line.rstrip())
                    elif brace_count == 0:
                        break
            
            # Remove the last closing brace
            while content and content[-1].strip() in ['', '}']:
                content.pop()
            
            return content
        
        elif extract_mode == "code_only":
            # Entire file minus using directives (auto-generated configs merge
            # whole files and hoist a shared set of usings to the top).
            using_re = re.compile(r'\s*using\s+(static\s+)?[\w.]+(\s*=\s*[\w.\s<>,]+)?\s*;\s*$')
            return [line.rstrip() for line in lines if not using_re.match(line)]

        else:
            # Default: return entire file
            return [line.rstrip() for line in lines]
    
    def process_build_order(self):
        """Process the build order and generate the output file"""
        for item in self.config.get('build_order', []):
            item_type = item.get('type')
            
            if item_type == 'header':
                # Add header comments
                for line in item.get('content', []):
                    self.output_lines.append(line)
                self.output_lines.append('')
            
            elif item_type == 'using':
                # Add using statements
                for statement in item.get('statements', []):
                    self.output_lines.append(f'using {statement};')
                self.output_lines.append('')
            
            elif item_type == 'namespace_open':
                # Open namespace
                namespace = self.config.get('namespace', 'DefaultNamespace')
                self.output_lines.append(f'namespace {namespace}')
                self.output_lines.append('{')
            
            elif item_type == 'class_open':
                # Open main class
                class_name = self.config.get('class', 'MainClass')
                base_class = self.config.get('base_class', '')
                if base_class:
                    self.output_lines.append(f'    public class {class_name} : {base_class}')
                else:
                    self.output_lines.append(f'    public class {class_name}')
                self.output_lines.append('    {')
            
            elif item_type == 'file':
                # Process a file
                rel_path = item.get('path', '')
                file_path = self.resolve_path(rel_path)
                extract_mode = item.get('extract', 'full')
                optional = item.get('optional', False)
                nested = item.get('nested', False)

                if not os.path.exists(file_path):
                    if optional:
                        continue
                    print(f"  ✗ Required file not found: {file_path}")
                    print(f"    (listed in {self.config_file})")
                    sys.exit(1)

                content = self.extract_content(file_path, extract_mode)

                if content:
                    # Add file marker comment
                    self.output_lines.append(f'        // --- From {rel_path} ---')

                    # Add content with proper indentation
                    indent = '        ' if nested else '        '
                    for line in content:
                        if line:
                            self.output_lines.append(indent + line)
                        else:
                            self.output_lines.append('')

                    self.output_lines.append('')
            
            elif item_type == 'class_close':
                # Close main class
                self.output_lines.append('    }')
            
            elif item_type == 'namespace_close':
                # Close namespace
                self.output_lines.append('}')
    
    def write_output(self):
        """Write the merged content to the output file"""
        output_file = self.resolve_path(self.config.get('output', 'output.cs'))

        with open(output_file, 'w', encoding='utf-8') as f:
            for line in self.output_lines:
                f.write(line + '\n')
        
        print(f"✓ Successfully built {output_file}")
        print(f"  Config: {self.config_file}")
        print(f"  Lines: {len(self.output_lines)}")
        
        # Show file size
        file_size = os.path.getsize(output_file)
        if file_size < 1024:
            print(f"  Size: {file_size} bytes")
        elif file_size < 1024 * 1024:
            print(f"  Size: {file_size / 1024:.2f} KB")
        else:
            print(f"  Size: {file_size / (1024 * 1024):.2f} MB")
    
    def build(self):
        """Main build process"""
        project_name = self.config.get('project', Path(self.config_file).stem)
        print(f"\n🔨 Building {project_name}...")
        
        self.process_build_order()
        self.write_output()
        
        # Optional: Run local compilation test
        if '--compile' in sys.argv:
            self.test_compilation()
    
    def test_compilation(self):
        """Compile-check the merged output via tools/check.ps1 (single source of truth)"""
        output_file = self.resolve_path(self.config.get('output', 'output.cs'))

        check_script = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'check.ps1')
        if not os.path.exists(check_script):
            print("  ✗ check.ps1 not found next to build_sansar.py")
            return

        import platform
        import subprocess

        powershell = 'powershell'
        if platform.system() == 'Linux':  # WSL
            powershell = 'powershell.exe'

        print("\nTesting local compilation...")
        result = subprocess.run(
            [powershell, '-NoProfile', '-ExecutionPolicy', 'Bypass',
             '-File', check_script, output_file],
            capture_output=True, text=True)

        if result.returncode == 0:
            print("  ✓ Compilation successful!")
        else:
            print("  ✗ Compilation failed:")
            print((result.stdout or result.stderr or '')[:2000])

class SmartBuilder:
    """Smart builder that can auto-discover and build multiple projects"""
    
    @staticmethod
    def find_build_configs(path: str = ".") -> List[str]:
        """Find all build configuration files"""
        configs = []
        
        # Common config file patterns
        patterns = [
            "**/build.json",
            "**/*-build.json",
            "**/sansar-project.json",
            "**/sansar-build.json"
        ]
        
        for pattern in patterns:
            configs.extend(glob.glob(os.path.join(path, pattern), recursive=True))
        
        # Remove duplicates and sort
        configs = sorted(list(set(configs)))
        return configs
    
    @staticmethod
    def detect_folder_structure(folder: str) -> Optional[str]:
        """Try to intelligently detect if a folder needs a build config"""
        # Check if folder exists
        if not os.path.isdir(folder):
            return None
        
        # Look for existing build config
        config_patterns = ['build.json', 'sansar-build.json', 'sansar-project.json', '*-build.json']
        for pattern in config_patterns:
            if '*' in pattern:
                # Handle glob patterns
                matches = glob.glob(os.path.join(folder, pattern))
                if matches:
                    return matches[0]  # Return first match
            else:
                config_path = os.path.join(folder, pattern)
                if os.path.exists(config_path):
                    return config_path
        
        # Check if this looks like a Sansar script folder
        cs_files = glob.glob(os.path.join(folder, "**/*.cs"), recursive=True)
        if cs_files:
            # Try to auto-generate a simple config
            return SmartBuilder.generate_simple_config(folder, cs_files)
        
        return None
    
    @staticmethod
    def generate_simple_config(folder: str, cs_files: List[str]) -> Optional[str]:
        """Generate a build config that merges a folder of .cs files"""
        # Never merge a previous merge output back into itself.
        cs_files = [f for f in cs_files if not f.endswith('-merged.cs')]
        if not cs_files:
            return None

        # Try to determine the main class file
        main_file = None
        for f in cs_files:
            with open(f, 'r', encoding='utf-8') as file:
                content = file.read()
                if ': SceneObjectScript' in content or 'public override void Init()' in content:
                    main_file = f
                    break

        if not main_file:
            print(f"⚠ Could not determine main class in {folder}")
            return None

        # Extract class name from main file
        class_name = Path(main_file).stem

        # Union of using directives across all files, kept verbatim; the merged
        # file gets one shared set at the top and 'code_only' strips the rest.
        using_re = re.compile(r'\s*using\s+(static\s+)?[\w.]+(\s*=\s*[\w.\s<>,]+)?\s*;\s*$')
        usings: List[str] = []
        for f in cs_files:
            with open(f, 'r', encoding='utf-8') as file:
                for line in file:
                    stripped = line.strip()
                    if using_re.match(line) and stripped not in usings:
                        usings.append(stripped)

        build_order: List[Dict[str, Any]] = [
            {"type": "header",
             "content": [f"// {class_name}-merged.cs - generated by build_sansar.py from {os.path.basename(os.path.abspath(folder))}/"]},
            {"type": "header", "content": usings},
        ]
        for f in cs_files:
            build_order.append({
                "type": "file",
                "path": os.path.relpath(f, folder).replace(os.sep, '/'),
                "extract": "code_only",
            })

        # Output name must not collide with any source file.
        config = {
            "project": class_name,
            "description": f"Auto-generated config for {class_name}",
            "version": "1.0.0",
            "output": f"{class_name}-merged.cs",
            "auto_generated": True,
            "build_order": build_order
        }

        # Save config
        config_path = os.path.join(folder, "build.json")
        with open(config_path, 'w') as f:
            json.dump(config, f, indent=2)

        print(f"📝 Generated config: {config_path}")
        return config_path

def main():
    """Main entry point with smart detection"""
    
    if len(sys.argv) == 1:
        # No arguments - auto-discover mode
        print("🔍 Auto-discovery mode: searching for build configurations...")
        configs = SmartBuilder.find_build_configs()
        
        if not configs:
            print("❌ No build configurations found!")
            print("\nCreate a build configuration file (e.g., build.json) or specify a folder.")
            sys.exit(1)
        
        print(f"\nFound {len(configs)} build configuration(s):")
        for i, config in enumerate(configs, 1):
            print(f"  {i}. {config}")
        
        if len(configs) == 1:
            # Only one config, build it
            builder = SansarBuilder(configs[0])
            builder.build()
        else:
            # Multiple configs, ask user
            print("\nOptions:")
            print("  Enter number to build specific config")
            print("  Enter 'all' to build all configs")
            print("  Enter 'q' to quit")
            
            choice = input("\nYour choice: ").strip().lower()
            
            if choice == 'q':
                sys.exit(0)
            elif choice == 'all' or choice == 'a':
                for config in configs:
                    builder = SansarBuilder(config)
                    builder.build()
            else:
                try:
                    idx = int(choice) - 1
                    if 0 <= idx < len(configs):
                        builder = SansarBuilder(configs[idx])
                        builder.build()
                    else:
                        print("❌ Invalid choice")
                except ValueError:
                    print("❌ Invalid input")
    
    elif sys.argv[1] in ['--all', '-a']:
        # Build all discovered configs
        configs = SmartBuilder.find_build_configs()
        print(f"Building {len(configs)} project(s)...")
        for config in configs:
            builder = SansarBuilder(config)
            builder.build()
    
    elif sys.argv[1] in ['--help', '-h']:
        print(__doc__)
    
    else:
        # Specific file or folder provided
        target = sys.argv[1]
        
        if os.path.isdir(target):
            # Folder mode - try to find or generate config
            print(f"📁 Folder mode: {target}")
            config = SmartBuilder.detect_folder_structure(target)
            
            if config:
                builder = SansarBuilder(config)
                builder.build()
            else:
                print(f"❌ Could not find or generate build config for {target}")
                sys.exit(1)
        
        elif os.path.isfile(target) and target.endswith('.json'):
            # Direct config file
            builder = SansarBuilder(target)
            builder.build()
        
        else:
            print(f"❌ Invalid target: {target}")
            print("Provide a JSON config file or a folder path")
            sys.exit(1)

if __name__ == "__main__":
    main()