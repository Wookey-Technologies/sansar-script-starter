using Sansar;
using Sansar.Script;
using Sansar.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace Minesweeper
{
    [RegisterReflective]
    class AudioResources : SceneObjectScript
    {
        [EditorVisible] private SoundResource SetupGame;
        [EditorVisible] private SoundResource StartGame;
        [EditorVisible] private SoundResource ResetGame;
        [EditorVisible] private SoundResource WinGame;

        [EditorVisible] private SoundResource FlagMine;
        [EditorVisible] private SoundResource UnflagMine;
        [EditorVisible] private SoundResource TripMine;

        [EditorVisible] private SoundResource RevealNumber;
        [EditorVisible] private SoundResource RevealEmpty;

        [EditorVisible] private SoundResource ConstructTile;

        [DefaultValue(80.0f)]
        [EditorVisible] private readonly float Loudness;

        [DefaultValue(2.0f)]
        [EditorVisible] private readonly float LoudnessVariance;

        [DefaultValue(0.0f)]
        [EditorVisible] private readonly float Pitch;

        [DefaultValue(3.0f)]
        [EditorVisible] private readonly float PitchVariance;

        Random _rnd = new Random();

        public override void Init() { }

        public void PlaySetupSound() { PlaySoundWithoutVariance(SetupGame, Loudness); }
        public void PlayStartSound() { PlaySoundWithoutVariance(StartGame, Loudness); }
        public void PlayResetSound() { PlaySoundWithoutVariance(ResetGame, Loudness); }

        public void PlayWinSound(Vector position) { PlaySoundWithVariance(WinGame, Loudness, 0.0f, 0.0f, 0.0f, position); }

        public void PlayFlagSound(Vector position) { PlaySoundWithVariance(FlagMine, Loudness, LoudnessVariance, Pitch, PitchVariance, position); }
        public void PlayUnflagSound(Vector position) { PlaySoundWithVariance(UnflagMine, Loudness, LoudnessVariance, Pitch, PitchVariance, position); }
        public void PlayTripSound(Vector position) { PlaySoundWithVariance(TripMine, Loudness, LoudnessVariance, Pitch, PitchVariance, position); }

        public void PlayRevealNumberSound(int number, Vector position) { PlaySoundWithVariance(RevealNumber, Loudness, LoudnessVariance, Pitch + 2.0f*number, PitchVariance, position); }
        public void PlayRevealEmptySound(Vector position) { PlaySoundWithVariance(RevealEmpty, Loudness, LoudnessVariance, Pitch, PitchVariance, position); }

        public void PlayConstructSound(Vector position, float pitchOffset) { PlaySoundWithVariance(ConstructTile, Loudness, LoudnessVariance, Pitch + pitchOffset, 0.0f, position); }

        private float RandomNegOneToOne()
        {
            return (float)(_rnd.NextDouble() * 2.0 - 1.0);
        }

        private float LoudnessPercentToDb(float loudnessPercent)
        {
            loudnessPercent = Math.Min(Math.Max(loudnessPercent, 0.0f), 100.0f);
            return 60.0f * (loudnessPercent / 100.0f) - 48.0f;
        }

        private void PlaySoundWithoutVariance(SoundResource sound, float loudness)
        {
            if (sound != null)
            {
                PlaySettings playSettings = PlaySettings.PlayOnce;
                playSettings.Loudness = LoudnessPercentToDb(loudness);

                ScenePrivate.PlaySound(sound, playSettings);
            }
        }

        private void PlaySoundWithVariance(SoundResource sound, float loudness, float loudnessVariance, float pitchOffset, float pitchVariance, Vector position)
        {
            if (sound != null)
            {
                PlaySettings playSettings = PlaySettings.PlayOnce;
                playSettings.Loudness = LoudnessPercentToDb(loudness + loudnessVariance * RandomNegOneToOne());
                playSettings.PitchShift = pitchOffset + pitchVariance * RandomNegOneToOne();

                ScenePrivate.PlaySoundAtPosition(sound, position, playSettings);
            }
        }
    }

    [RegisterReflective]
    class BoardResources : SceneObjectScript
    {
        [EditorVisible] private ClusterResource BlockEmpty;
        [EditorVisible] private ClusterResource Block1;
        [EditorVisible] private ClusterResource Block2;
        [EditorVisible] private ClusterResource Block3;
        [EditorVisible] private ClusterResource Block4;
        [EditorVisible] private ClusterResource Block5;
        [EditorVisible] private ClusterResource Block6;
        [EditorVisible] private ClusterResource Block7;
        [EditorVisible] private ClusterResource Block8;
        [EditorVisible] private ClusterResource BlockMine;
        [EditorVisible] private ClusterResource BlockMineRed;

        [EditorVisible] private ClusterResource TileEmpty;
        [EditorVisible] private ClusterResource TileMarked;

        [EditorVisible] private ClusterResource TileLose;
        [EditorVisible] private ClusterResource TileWin;

        ClusterResource[] _blocks = new ClusterResource[9];
        ClusterResource[] _blockMines = new ClusterResource[2];
        ClusterResource[] _tiles = new ClusterResource[4];

        public override void Init()
        {
            _blocks[0] = BlockEmpty;
            _blocks[1] = Block1;
            _blocks[2] = Block2;
            _blocks[3] = Block3;
            _blocks[4] = Block4;
            _blocks[5] = Block5;
            _blocks[6] = Block6;
            _blocks[7] = Block7;
            _blocks[8] = Block8;

            _blockMines[0] = BlockMine;
            _blockMines[1] = BlockMineRed;

            _tiles[0] = TileEmpty;
            _tiles[1] = TileMarked;
            _tiles[2] = TileLose;
            _tiles[3] = TileWin;
        }

        public ClusterResource Block(int i) { return _blocks[i]; }
        public ClusterResource Mines(bool tripped) { return (tripped ? _blockMines[1] : _blockMines[0]); }
        public ClusterResource Tiles(bool marked) { return (marked ? _tiles[1] : _tiles[0]); }
        public ClusterResource GameOverTile(bool win) { return (win ? _tiles[3] : _tiles[2]); }
    }

    [RegisterReflective]
    class ScoreResources : SceneObjectScript
    {
        [EditorVisible] private ClusterResource OnesDigit;
        [EditorVisible] private ClusterResource TensDigit;

        public override void Init() { }

        public ClusterResource Digit(bool tens) { return (tens ? TensDigit : OnesDigit); }
    }

    [RegisterReflective]
    class GameManager : SceneObjectScript
    {
        public Vector FirstCornerPosition;

        [DefaultValue(36)]
        public readonly int MaxGridSize;

        AudioResources _audioResources = null;
        BoardResources _modelResources = null;
        ScoreResources _scoreResources = null;

        Random _rnd = new Random();

        Vector[] _cornerPositions = new Vector[8];
        bool[] _boardOccupied = new bool[8];

        Dictionary<Game, int> _activeGames = new Dictionary<Game, int>();

        HashSet<AgentInfo> _usersRegistered = new HashSet<AgentInfo>();

        public override void Init()
        {
            _audioResources = ScenePrivate.FindReflective<AudioResources>("Minesweeper.AudioResources").FirstOrDefault();
            _modelResources = ScenePrivate.FindReflective<BoardResources>("Minesweeper.BoardResources").FirstOrDefault();
            _scoreResources = ScenePrivate.FindReflective<ScoreResources>("Minesweeper.ScoreResources").FirstOrDefault();

            _cornerPositions[0] = FirstCornerPosition + new Vector( 0.0f * MaxGridSize, 0.0f, 0.0f);
            _cornerPositions[1] = FirstCornerPosition + new Vector(-1.0f * MaxGridSize, 0.0f, 0.0f);
            _cornerPositions[2] = FirstCornerPosition + new Vector(-2.0f * MaxGridSize, 0.0f, 0.0f);
            _cornerPositions[3] = FirstCornerPosition + new Vector( 0.0f * MaxGridSize, 1.0f * MaxGridSize, 0.0f);
            _cornerPositions[4] = FirstCornerPosition + new Vector(-2.0f * MaxGridSize, 1.0f * MaxGridSize, 0.0f);
            _cornerPositions[5] = FirstCornerPosition + new Vector( 0.0f * MaxGridSize, 2.0f * MaxGridSize, 0.0f);
            _cornerPositions[6] = FirstCornerPosition + new Vector(-1.0f * MaxGridSize, 2.0f * MaxGridSize, 0.0f);
            _cornerPositions[7] = FirstCornerPosition + new Vector(-2.0f * MaxGridSize, 2.0f * MaxGridSize, 0.0f);

            for (int i = 0; i < 8; i++)
                _boardOccupied[i] = false;

            ScenePrivate.User.Subscribe(User.AddUser, (UserData ud) =>
            {
                SetupCommandsForAgent(ScenePrivate.FindAgent(ud.User));
            });

            foreach (var agent in ScenePrivate.GetAgents())
            {
                SetupCommandsForAgent(agent);
            }
        }

        void SetupCommandsForAgent(AgentPrivate agent)
        {
            if (agent != null)
            {
                if (_usersRegistered.Contains(agent.AgentInfo))
                    return;

                _usersRegistered.Add(agent.AgentInfo);

                agent.Client.SubscribeToCommand("Trigger", CommandAction.Pressed, OnLeftClick, (d) => { });
                //agent.Client.SubscribeToCommand("PrimaryAction", CommandAction.Pressed, OnLeftClick, (d) => { });
                agent.Client.SubscribeToCommand("SecondaryAction", CommandAction.Pressed, OnRightClick, (d) => { });
            }
        }

        void OnLeftClick(CommandData command)
        {
            if (command.Action != CommandAction.Pressed)
                return;

            if (command.TargetingNormal.X != 0.0f || command.TargetingNormal.Y != 0.0f || command.TargetingNormal.Z != 0.0f)
            {
                //try
                {
                    ObjectPrivate targetObject = ScenePrivate.FindObject(command.TargetingComponent.ObjectId);
                    if (targetObject != null)
                    {
                        UncoverTile(targetObject);
                    }
                }
                //catch { } // ignore exceptions
            }
        }

        void OnRightClick(CommandData command)
        {
            if (command.Action != CommandAction.Pressed)
                return;

            if (command.TargetingNormal.X != 0.0f || command.TargetingNormal.Y != 0.0f || command.TargetingNormal.Z != 0.0f)
            {
                //try
                {
                    ObjectPrivate targetObject = ScenePrivate.FindObject(command.TargetingComponent.ObjectId);
                    if (targetObject != null)
                    {
                        FlagTile(targetObject);
                    }
                }
                //catch { } // ignore exceptions
            }
        }

        void ResetGameAfterTimeout(Game game)
        {
            if (_activeGames.ContainsKey(game))
            {
                int boardIndex = _activeGames[game];

                _activeGames.Remove(game);

                Wait(TimeSpan.FromSeconds(15.0));

                game.DestroyBoard();

                _boardOccupied[boardIndex] = false;
            }
        }

        int GetAvailableSlot()
        {
            for (int i = 0; i < 8; i++)
            {
                if (_boardOccupied[i] == false)
                    return i;
            }

            return -1;
        }

        public void SetupGame(AgentPrivate agent, int width, int height, int mineCount, bool wrapWidth, bool wrapHeight, float spinSpeed, float explosionPower, Action completionCallback)
        {
            Game.GameConfig config = new Game.GameConfig { GridWidth = width, GridHeight = height, MineCount = mineCount, WrapHorizontal = wrapWidth, WrapVertical = wrapHeight, SpinSpeed = spinSpeed, ExplosionPower = explosionPower };

            config.GridWidth = Math.Min(MaxGridSize, config.GridWidth);
            config.GridHeight = Math.Min(MaxGridSize, config.GridHeight);

            if (config.GridWidth <= 0 || config.GridHeight <= 0)
            {
                Log.Write($"Can't create game board of size {config.GridWidth}x{config.GridHeight}!");
                completionCallback();
                return;
            }

            int availableIndex = GetAvailableSlot();
            if (availableIndex < 0)
            {
                _audioResources.PlayTripSound(ObjectPrivate.Position);
                completionCallback();
                return;
            }

            _boardOccupied[availableIndex] = true;

            Game game = new Game(this, _audioResources, _modelResources, _scoreResources, config, _cornerPositions[availableIndex], _rnd);

            _activeGames.Add(game, availableIndex);

            try
            {
                agent.Client.TeleportTo(game.GetSafePosition() + Vector.ObjectUp, new Vector(0.0f, -1.0f, 0.0f));
            }
            catch
            {
            }

            completionCallback();
        }

        public void FlagTile(ObjectPrivate potentialTileObject)
        {
            foreach (var game in _activeGames.Keys)
            {
                if (game.FlagTile(this, potentialTileObject))
                    return;
            }
        }

        public void UncoverTile(ObjectPrivate potentialTileObject)
        {
            foreach (var game in _activeGames.Keys)
            {
                if (game.UncoverTile(this, potentialTileObject))
                    return;
            }
        }

        public ScenePrivate.CreateClusterData CreateClusterPassthrough(ClusterResource cr, Vector pos, Quaternion rot)
        {
            ScenePrivate.CreateClusterData ccd = null;

            bool created = false;

            while (!created)
            {
                try
                {
                    ccd = WaitFor(ScenePrivate.CreateCluster, cr, pos, rot, Vector.Zero) as ScenePrivate.CreateClusterData;
                    created = true;
                }
                catch (ThrottleException)
                {
                    Wait(TimeSpan.FromSeconds(0.5));
                }
            }

            return ccd;
        }

        public List<RigidBodyComponent> RigidBodiesNear(Vector pos, float radius, Vector direction)
        {
            RayCastHit[] castHits = ScenePrivate.CastSphere(radius, pos, pos + direction * 1.2f * radius, ScenePrivate.MaximumCastRayResults);
            //Log.Write("castHits: " + castHits.Length);

            List<RigidBodyComponent> rbs = new List<RigidBodyComponent>();

            for (int i = 0; i < castHits.Length; i++)
            {
                var agent = ScenePrivate.FindAgent(castHits[i].ComponentId.ObjectId);
                if (agent != null)
                {
                    //Log.Write("Raycast found agent, ignoring...");
                    continue;
                }

                var obj = ScenePrivate.FindObject(castHits[i].ComponentId.ObjectId);
                if (obj == null)
                {
                    //Log.Write("Raycast found null object, ignoring...");
                    continue;
                }

                RigidBodyComponent rb = obj.GetComponent(ComponentType.RigidBodyComponent, 0) as RigidBodyComponent;
                if (rb != null)
                {
                    if (rb.GetMotionType() != RigidBodyMotionType.MotionTypeStatic)
                        rbs.Add(rb);
                }
                //else
                //{
                //    Log.Write("Raycast found object with null rigidbody, ignoring...");
                //}
            }

            //Log.Write("Raycast non-null rbs: " + rbs.Count);

            return rbs;
        }

        public string RigidbodyName(RigidBodyComponent rb)
        {
            if (rb == null)
                return "null";

            ObjectPrivate obj = ScenePrivate.FindObject(rb.ComponentId.ObjectId);
            return obj.Name;
        }

        public void LogPassthrough(string message)
        {
            Log.Write(message);
        }

        public void WaitPassthrough(float seconds)
        {
            Wait(TimeSpan.FromSeconds(seconds));
        }

        class Game
        {
            public enum BoardState
            {
                EMPTY = 0,
                ONE = 1,
                TWO = 2,
                THREE = 3,
                FOUR = 4,
                FIVE = 5,
                SIX = 6,
                SEVEN = 7,
                EIGHT = 8,
                MINE = 9,
            }

            public enum TileState
            {
                EMPTY,
                COVERED,
                FLAGGED,
            }

            public struct GameConfig { public int GridWidth; public int GridHeight; public int MineCount; public bool WrapHorizontal; public bool WrapVertical; public float SpinSpeed; public float ExplosionPower; }

            struct TileIndex { public int X; public int Y; }

            ScenePrivate.CreateClusterData[,] _boardCubes = null;
            ScenePrivate.CreateClusterData[,] _coverTiles = null;
            ScenePrivate.CreateClusterData[] _scoreTiles = null;
            ScenePrivate.CreateClusterData[] _scoreCounters = null;
            AnimationComponent[] _scoreAnimComps = null;

            Vector _scoreTileOutcomePos;
            Quaternion _scoreTileOutcomeRot;

            Dictionary<ObjectPrivate, TileIndex> _tileObjects = null;

            BoardState[,] _board = null;
            TileState[,] _covers = null;

            AudioResources _audioResources = null;
            BoardResources _modelResources = null;

            GameConfig _config;

            Vector _cornerPosition;

            int _remainingFlagCount;

            Vector BoardPosition(int x, int y)
            {
                float xpos = (float)x;
                float ypos = (float)y;
                float zpos = 0.0f;

                if (_config.WrapHorizontal)
                {
                    float angle = x * Mathf.TwoPi / _config.GridWidth;
                    float radius = 0.5f / (2.0f * (float)Math.Sin(Mathf.PiOverTwo / _config.GridWidth));
                    zpos = radius * (1.0f - (float)Math.Cos(angle));
                    xpos = radius * (float)Math.Sin(angle);
                }
                else if (_config.WrapVertical)
                {
                    float angle = y * Mathf.TwoPi / _config.GridHeight;
                    float radius = 0.5f / (2.0f * (float)Math.Sin(Mathf.PiOverTwo / _config.GridHeight));
                    zpos = radius * (1.0f - (float)Math.Cos(angle));
                    ypos = radius * (float)Math.Sin(angle);
                }

                return _cornerPosition + new Vector(-xpos, -ypos, zpos);
            }

            Quaternion BoardRotation(int x, int y)
            {
                if (_config.WrapHorizontal)
                {
                    float angle = x * Mathf.TwoPi / _config.GridWidth;
                    return Quaternion.FromEulerAngles(new Vector(0.0f, angle, 0.0f));
                }
                else if (_config.WrapVertical)
                {
                    float angle = y * Mathf.TwoPi / _config.GridWidth;
                    return Quaternion.FromEulerAngles(new Vector(-angle, 0.0f, 0.0f));
                }

                return Quaternion.Identity;
            }

            public Game(GameManager gm, AudioResources audioResources, BoardResources modelResources, ScoreResources scoreResources, GameConfig config, Vector firstCornerPosition, Random rnd)
            {
                _audioResources = audioResources;
                _modelResources = modelResources;

                _cornerPosition = firstCornerPosition;

                _config = config;

                _audioResources.PlaySetupSound();

                CreateCovers();
                CreateBoard(rnd);

                SetupTiles(gm, modelResources);
                SetupBoard(gm, modelResources);

                if (_config.WrapHorizontal && (_config.SpinSpeed > 0.0f))
                    StartSpinning(gm);

                SetupScoreTiles(gm, modelResources, scoreResources);

                _remainingFlagCount = _config.MineCount;
                UpdateFlagCounter();

                _audioResources.PlayStartSound();
            }

            public Vector GetSafePosition()
            {
                if (_config.WrapHorizontal)
                {
                    for (int x = 0; x < _config.GridWidth; x++)
                        for (int y = _config.GridHeight / 2; y < _config.GridHeight; y++)
                            if (_board[x, y] != BoardState.MINE)
                                return BoardPosition(x, y);
                }
                else if (_config.WrapVertical)
                {
                    for (int x = _config.GridWidth / 2; x < _config.GridWidth; x++)
                        for (int y = 0; y < _config.GridHeight; y++)
                            if (_board[x, y] != BoardState.MINE)
                                return BoardPosition(x, y);
                }
                else
                {
                    for (int x = _config.GridWidth / 2; x < _config.GridWidth; x++)
                        for (int y = _config.GridHeight / 2; y < _config.GridHeight; y++)
                            if (_board[x, y] != BoardState.MINE)
                                return BoardPosition(x, y);
                }

                return BoardPosition(0, 0);
            }

            void StartSpinning(GameManager gm)
            {
                float radius = 0.5f / (2.0f * (float)Math.Sin(Mathf.PiOverTwo / _config.GridWidth));
                Vector comOffset = Vector.ObjectUp * radius;
                Vector angularVel = new Vector(0.0f, _config.SpinSpeed, 0.0f);

                for (int x = 0; x < _config.GridWidth; x++)
                {
                    for (int y = 0; y < _config.GridHeight; y++)
                    {
                        RigidBodyComponent brb = _boardCubes[x, y].ClusterReference.GetObjectPrivate(0).GetComponent(ComponentType.RigidBodyComponent, 0) as RigidBodyComponent;
                        RigidBodyComponent trb = _coverTiles[x, y].ClusterReference.GetObjectPrivate(0).GetComponent(ComponentType.RigidBodyComponent, 0) as RigidBodyComponent;

                        brb.SetCenterOfMass(comOffset);
                        trb.SetCenterOfMass(comOffset);
                    }
                }

                gm.WaitPassthrough(1.0f);

                for (int x = 0; x < _config.GridWidth; x++)
                {
                    for (int y = 0; y < _config.GridHeight; y++)
                    {
                        RigidBodyComponent brb = _boardCubes[x, y].ClusterReference.GetObjectPrivate(0).GetComponent(ComponentType.RigidBodyComponent, 0) as RigidBodyComponent;
                        RigidBodyComponent trb = _coverTiles[x, y].ClusterReference.GetObjectPrivate(0).GetComponent(ComponentType.RigidBodyComponent, 0) as RigidBodyComponent;

                        brb.SetAngularVelocity(angularVel);
                        trb.SetAngularVelocity(angularVel);
                    }
                }
            }

            public void DestroyBoard()
            {
                _audioResources.PlayResetSound();

                _tileObjects = null;

                for (int x = 0; x < _config.GridWidth; x++)
                {
                    for (int y = 0; y < _config.GridHeight; y++)
                    {
                        if (_boardCubes[x, y] != null)
                            _boardCubes[x, y].ClusterReference.Destroy();
                        if (_coverTiles[x, y] != null)
                            _coverTiles[x, y].ClusterReference.Destroy();
                    }
                }

                _boardCubes = null;
                _coverTiles = null;

                if (_scoreTiles != null)
                {
                    if (_scoreTiles[0] != null)
                        _scoreTiles[0].ClusterReference.Destroy();
                    if (_scoreTiles[1] != null)
                        _scoreTiles[1].ClusterReference.Destroy();
                }

                if (_scoreCounters != null)
                {
                    if (_scoreCounters[0] != null)
                        _scoreCounters[0].ClusterReference.Destroy();
                    if (_scoreCounters[1] != null)
                        _scoreCounters[1].ClusterReference.Destroy();
                }
            }

            void CreateBoard(Random rnd)
            {
                _board = new BoardState[_config.GridWidth, _config.GridHeight];

                // Randomly place mines
                int count = 0;
                while (count < _config.MineCount)
                {
                    int rndX = rnd.Next(_config.GridWidth);
                    int rndY = rnd.Next(_config.GridHeight);

                    if (_board[rndX, rndY] == BoardState.EMPTY)
                    {
                        _board[rndX, rndY] = BoardState.MINE;
                        count += 1;
                    }
                }

                // Populate counts
                for (int x = 0; x < _config.GridWidth; x++)
                {
                    int left = (x > 0 ? x - 1 : x);
                    int right = (x < _config.GridWidth - 1 ? x + 1 : x);

                    for (int y = 0; y < _config.GridHeight; y++)
                    {
                        if (_board[x, y] == BoardState.MINE)
                            continue;

                        int bottom = (y > 0 ? y - 1 : y);
                        int top = (y < _config.GridHeight - 1 ? y + 1 : y);

                        int neighboringMines = 0;

                        for (int i = left; i <= right; i++)
                            for (int j = bottom; j <= top; j++)
                                if (_board[i, j] == BoardState.MINE)
                                    neighboringMines += 1;

                        if (_config.WrapHorizontal && ((x == 0) || (x == _config.GridWidth - 1)))
                        {
                            int i = 0;
                            if (x == 0)
                                i = _config.GridWidth - 1;

                            for (int j = bottom; j <= top; j++)
                                if (_board[i, j] == BoardState.MINE)
                                    neighboringMines += 1;
                        }

                        if (_config.WrapVertical && ((y == 0) || (y == _config.GridHeight - 1)))
                        {
                            int j = 0;
                            if (y == 0)
                                j = _config.GridHeight - 1;

                            for (int i = left; i <= right; i++)
                                if (_board[i, j] == BoardState.MINE)
                                    neighboringMines += 1;
                        }

                        _board[x, y] = (BoardState)neighboringMines;
                    }
                }
            }

            void SetupBoard(GameManager gm, BoardResources modelResources)
            {
                // Create board clusters
                _boardCubes = new ScenePrivate.CreateClusterData[_config.GridWidth, _config.GridHeight];

                for (int x = 0; x < _config.GridWidth; x++)
                {
                    _audioResources.PlayConstructSound(BoardPosition(x, _config.GridHeight / 2), -6.0f);

                    for (int y = 0; y < _config.GridHeight; y++)
                    {
                        BoardState bs = _board[x, y];
                        ClusterResource cr = (bs == BoardState.MINE ? modelResources.Mines(false) : modelResources.Block((int)bs));
                        _boardCubes[x, y] = gm.CreateClusterPassthrough(cr, BoardPosition(x, y), BoardRotation(x, y));
                    }
                }
            }

            void SetupScoreTiles(GameManager gm, BoardResources modelResources, ScoreResources scoreResources)
            {
                Vector pos = BoardPosition(_config.GridWidth / 2, _config.GridHeight) + new Vector(0.0f, 0.0f, 0.3f);
                Quaternion rot = Quaternion.FromEulerAngles(new Vector(-0.707f, 0.0f, 0.0f));

                float baseOffset = ((_config.GridWidth % 2) == 0) ? 1.0f : 0.5f;

                if (_config.WrapHorizontal)
                {
                    baseOffset = 0.5f;
                    pos = BoardPosition(0, _config.GridHeight) + new Vector(0.0f, 0.0f, 0.3f);
                }

                _scoreTileOutcomePos = pos + new Vector(baseOffset - 1.0f, 0.0f, 0.0f);
                _scoreTileOutcomeRot = rot;

                _scoreTiles = new ScenePrivate.CreateClusterData[2];
                _scoreTiles[0] = gm.CreateClusterPassthrough(modelResources.Tiles(true), pos + new Vector(baseOffset, 0.0f, 0.0f), rot);
                _scoreTiles[1] = gm.CreateClusterPassthrough(modelResources.Tiles(false), _scoreTileOutcomePos, rot);

                rot = Quaternion.FromEulerAngles(new Vector(0.84f, 0.0f, 0.0f));
                _scoreCounters = new ScenePrivate.CreateClusterData[2];
                _scoreCounters[0] = gm.CreateClusterPassthrough(scoreResources.Digit(false), _scoreTileOutcomePos + new Vector(-0.16f, -0.09f, -0.075f), rot);
                _scoreCounters[1] = gm.CreateClusterPassthrough(scoreResources.Digit(true), _scoreTileOutcomePos + new Vector(0.24f, -0.09f, -0.075f), rot);

                _scoreAnimComps = new AnimationComponent[2];
                _scoreAnimComps[0] = (AnimationComponent)_scoreCounters[0].ClusterReference.GetObjectPrivate(0).GetComponent(ComponentType.AnimationComponent, 0);
                _scoreAnimComps[1] = (AnimationComponent)_scoreCounters[1].ClusterReference.GetObjectPrivate(0).GetComponent(ComponentType.AnimationComponent, 0);
            }

            readonly int[] powersOfTen = new int[] { 1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000 };

            public void UpdateFlagCounter()
            {
                for (int digit = 0; digit < 2; digit++)
                {
                    if (digit < 0 || digit >= powersOfTen.Length)
                        return;

                    int largeOrder = powersOfTen[digit + 1];
                    int order = powersOfTen[digit];

                    int d = ((_remainingFlagCount % largeOrder) / order);

                    _scoreAnimComps[digit].DefaultAnimation.JumpToFrame(2 * d + 1);
                }
            }

            void CreateCovers()
            {
                _covers = new TileState[_config.GridWidth, _config.GridHeight];

                for (int x = 0; x < _config.GridWidth; x++)
                    for (int y = 0; y < _config.GridHeight; y++)
                        _covers[x, y] = TileState.COVERED;
            }

            void SetupTiles(GameManager gm, BoardResources modelResources)
            {
                _coverTiles = new ScenePrivate.CreateClusterData[_config.GridWidth, _config.GridHeight];
                _tileObjects = new Dictionary<ObjectPrivate, TileIndex>();

                for (int x = 0; x < _config.GridWidth; x++)
                {
                    _audioResources.PlayConstructSound(BoardPosition(x, _config.GridHeight / 2), 0.0f);

                    for (int y = 0; y < _config.GridHeight; y++)
                    {
                        SetupTile(gm, x, y, modelResources.Tiles(false));
                    }
                }
            }

            void SetupTile(GameManager gm, int x, int y, ClusterResource tile)
            {
                Cluster previousTile = null;
                if (_coverTiles[x, y] != null)
                    previousTile = _coverTiles[x, y].ClusterReference;

                _coverTiles[x, y] = gm.CreateClusterPassthrough(tile, BoardPosition(x, y), BoardRotation(x, y));

                ObjectPrivate tileObject = _coverTiles[x, y].ClusterReference.GetObjectPrivate(0);
                if (tileObject != null)
                    _tileObjects[tileObject] = new TileIndex { X = x, Y = y };
                else
                    gm.LogPassthrough("Couldn't get cluster reference object 0 for tile!");

                if (previousTile != null)
                    previousTile.Destroy();
            }

            public bool FlagTile(GameManager gm, ObjectPrivate potentialTileObject)
            {
                if (_tileObjects == null)
                    return false;

                if (_tileObjects.ContainsKey(potentialTileObject))
                {
                    TileIndex ti = _tileObjects[potentialTileObject];

                    if (FlagTile(gm, ti.X, ti.Y))
                    {
                        _tileObjects.Remove(potentialTileObject);
                    }

                    return true;
                }

                return false;
            }

            bool FlagTile(GameManager gm, int x, int y)
            {
                if (_covers[x, y] == TileState.COVERED)
                {
                    if (_remainingFlagCount <= 0)
                    {
                        _audioResources.PlayConstructSound(BoardPosition(x, y), -3.0f);
                        return false;
                    }

                    _remainingFlagCount--;

                    _covers[x, y] = TileState.FLAGGED;

                    _audioResources.PlayFlagSound(BoardPosition(x, y));

                    SetupTile(gm, x, y, _modelResources.Tiles(true));
                }
                else if (_covers[x, y] == TileState.FLAGGED)
                {
                    _remainingFlagCount++;

                    _covers[x, y] = TileState.COVERED;

                    _audioResources.PlayUnflagSound(BoardPosition(x, y));

                    SetupTile(gm, x, y, _modelResources.Tiles(false));
                }

                UpdateFlagCounter();

                return true;
            }

            public bool UncoverTile(GameManager gm, ObjectPrivate potentialTileObject)
            {
                if (_tileObjects == null)
                    return false;

                if (_tileObjects.ContainsKey(potentialTileObject))
                {
                    TileIndex ti = _tileObjects[potentialTileObject];

                    UncoverTile(gm, ti.X, ti.Y, potentialTileObject);

                    return true;
                }

                return false;
            }

            void UncoverTile(GameManager gm, int x, int y, ObjectPrivate potentialTileObject)
            {
                if (_covers[x, y] == TileState.COVERED)
                {
                    _covers[x, y] = TileState.EMPTY;

                    _coverTiles[x, y].ClusterReference.Destroy();
                    _coverTiles[x, y] = null;

                    _tileObjects.Remove(potentialTileObject);

                    if (_board[x, y] == BoardState.MINE)
                    {
                        // GAME OVER
                        _boardCubes[x, y].ClusterReference.Destroy();
                        _boardCubes[x, y] = gm.CreateClusterPassthrough(_modelResources.Mines(true), BoardPosition(x, y), BoardRotation(x, y));

                        _scoreCounters[0].ClusterReference.Destroy();
                        _scoreCounters[1].ClusterReference.Destroy();
                        _scoreCounters = null;

                        _scoreTiles[1].ClusterReference.Destroy();
                        _scoreTiles[1] = gm.CreateClusterPassthrough(_modelResources.GameOverTile(false), _scoreTileOutcomePos, _scoreTileOutcomeRot);

                        _audioResources.PlayTripSound(BoardPosition(x, y));

                        RevealAllMinesAndDisable();

                        if (_config.ExplosionPower > 0.0f)
                            Explodify(gm, x, y);

                        gm.ResetGameAfterTimeout(this);
                    }
                    else
                    {
                        if (_board[x, y] == BoardState.EMPTY)
                        {
                            _audioResources.PlayRevealEmptySound(BoardPosition(x, y));

                            UncoverEmptyRegion(x, y);
                        }
                        else
                        {
                            _audioResources.PlayRevealNumberSound((int)_board[x, y], BoardPosition(x, y));
                        }

                        if (CheckForWin())
                        {
                            // GAME WIN
                            _scoreCounters[0].ClusterReference.Destroy();
                            _scoreCounters[1].ClusterReference.Destroy();
                            _scoreCounters = null;

                            _scoreTiles[1].ClusterReference.Destroy();
                            _scoreTiles[1] = gm.CreateClusterPassthrough(_modelResources.GameOverTile(true), _scoreTileOutcomePos, _scoreTileOutcomeRot);

                            _audioResources.PlayWinSound(BoardPosition(_config.GridWidth / 2, _config.GridHeight / 2));

                            gm.ResetGameAfterTimeout(this);
                        }
                    }
                }
                else if (_covers[x, y] == TileState.FLAGGED)
                {
                    _audioResources.PlayConstructSound(BoardPosition(x, y), 0.0f);
                }
            }

            void UncoverEmptyRegion(int x, int y)
            {
                // It's empty, uncover all covered neighbors

                int left = (x > 0 ? x - 1 : x);
                int right = (x < _config.GridWidth - 1 ? x + 1 : x);

                int bottom = (y > 0 ? y - 1 : y);
                int top = (y < _config.GridHeight - 1 ? y + 1 : y);

                HashSet<TileIndex> freshlyUncovered = new HashSet<TileIndex>();

                for (int i = left; i <= right; i++)
                {
                    for (int j = bottom; j <= top; j++)
                    {
                        if (_covers[i, j] == TileState.COVERED)
                        {
                            _covers[i, j] = TileState.EMPTY;

                            _coverTiles[i, j].ClusterReference.Destroy();
                            _coverTiles[i, j] = null;

                            if (_board[i, j] == BoardState.EMPTY)
                                freshlyUncovered.Add(new TileIndex { X = i, Y = j });
                        }
                    }
                }

                if (_config.WrapHorizontal && ((x == 0) || (x == _config.GridWidth - 1)))
                {
                    int i = 0;
                    if (x == 0)
                        i = _config.GridWidth - 1;

                    for (int j = bottom; j <= top; j++)
                    {
                        if (_covers[i, j] == TileState.COVERED)
                        {
                            _covers[i, j] = TileState.EMPTY;

                            _coverTiles[i, j].ClusterReference.Destroy();
                            _coverTiles[i, j] = null;

                            if (_board[i, j] == BoardState.EMPTY)
                                freshlyUncovered.Add(new TileIndex { X = i, Y = j });
                        }
                    }
                }

                if (_config.WrapVertical && ((y == 0) || (y == _config.GridHeight - 1)))
                {
                    int j = 0;
                    if (y == 0)
                        j = _config.GridHeight - 1;

                    for (int i = left; i <= right; i++)
                    {
                        if (_covers[i, j] == TileState.COVERED)
                        {
                            _covers[i, j] = TileState.EMPTY;

                            _coverTiles[i, j].ClusterReference.Destroy();
                            _coverTiles[i, j] = null;

                            if (_board[i, j] == BoardState.EMPTY)
                                freshlyUncovered.Add(new TileIndex { X = i, Y = j });
                        }
                    }
                }

                foreach (var tileIndex in freshlyUncovered)
                    UncoverEmptyRegion(tileIndex.X, tileIndex.Y);
            }

            void RevealAllMinesAndDisable()
            {
                for (int x = 0; x < _config.GridWidth; x++)
                {
                    for (int y = 0; y < _config.GridHeight; y++)
                    {
                        if ((_board[x, y] == BoardState.MINE) && (_covers[x, y] != TileState.FLAGGED) ||
                            (_board[x, y] != BoardState.MINE) && (_covers[x, y] == TileState.FLAGGED))
                        {
                            _covers[x, y] = TileState.EMPTY;

                            if (_coverTiles[x, y] != null)
                            {
                                _coverTiles[x, y].ClusterReference.Destroy();
                                _coverTiles[x, y] = null;
                            }
                        }
                    }
                }

                _tileObjects = null;
            }

            void Explodify(GameManager gm, int explodeX, int explodeY)
            {
                const float explosionRadius = 8.0f;

                Vector objectUp = _boardCubes[explodeX, explodeY].ClusterReference.GetObjectPrivate(0).UpVector;
                Vector centerOfExplosion = BoardPosition(explodeX, explodeY) - objectUp * 1.1f * explosionRadius;
                List<RigidBodyComponent> nearbyRbs = gm.RigidBodiesNear(centerOfExplosion, explosionRadius, objectUp);

                foreach (var rb in nearbyRbs)
                {
                    try
                    {
                        rb.SetMotionType(RigidBodyMotionType.MotionTypeDynamic);

                        Vector fromCenter = rb.GetPosition() - centerOfExplosion;
                        float distanceSq = Math.Max(1.0f, fromCenter.LengthSquared());
                        Vector impulse = fromCenter.Normalized() * _config.ExplosionPower / distanceSq;

                        rb.AddLinearImpulse(impulse);
                    }
                    catch
                    {
                        //gm.LogPassthrough($"Unable to set motion type on {gm.RigidbodyName(rb)}");
                    }
                }
            }

            bool CheckForWin()
            {
                for (int x = 0; x < _config.GridWidth; x++)
                {
                    for (int y = 0; y < _config.GridHeight; y++)
                    {
                        if ((_board[x, y] != BoardState.MINE) && (_covers[x, y] != TileState.EMPTY))
                        {
                            return false;
                        }
                    }
                }

                _tileObjects = null;

                return true;
            }
        }
    }

    [DefaultScript]
    class GameSetup : SceneObjectScript
    {
        // Beginner (8x8, 10 mines), Intermediate (16x16, 40 mines) and Expert (24x24, 99 mines) Expert (16x30)

        [EditorVisible] private Interaction SetupGameInteraction;

        [EditorVisible] private readonly int Width = 9;
        [EditorVisible] private readonly int Height = 9;

        [EditorVisible] private readonly int MineCount = 13;

        [EditorVisible] private readonly bool WrapHorizontal = false;
        //[EditorVisible] private readonly bool WrapVertical = false;

        [EditorVisible] private readonly float SpinSpeed = 0.0f;

        [EditorVisible] private readonly float ExplosionPower = 0.0f;

        private GameManager _gameManager;

        public override void Init()
        {
            _gameManager = ScenePrivate.FindReflective<GameManager>("Minesweeper.GameManager").FirstOrDefault();

            SetupGameInteraction.Subscribe((InteractionData data) =>
            {
                WaitFor(SetupGameInteraction.SetEnabled, false);

                AgentPrivate agent = ScenePrivate.FindAgent(data.AgentId);

                _gameManager.SetupGame(agent, Width, Height, MineCount, WrapHorizontal, false, SpinSpeed, ExplosionPower, () => { SetupGameInteraction.SetEnabled(true); });
            });
        }
    }
}
