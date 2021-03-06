﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace coders_of_the_caribbean_referee_dotnet
{

    public class Referee
    {
        private const int LEAGUE_LEVEL = 3;

        private const int MAP_WIDTH = 23;
        private const int MAP_HEIGHT = 21;
        private const int COOLDOWN_CANNON = 2;
        private const int COOLDOWN_MINE = 5;
        private const int INITIAL_SHIP_HEALTH = 100;
        private const int MAX_SHIP_HEALTH = 100;
        private const int MIN_SHIPS = 1;
        private const int MIN_RUM_BARRELS = 10;
        private const int MAX_RUM_BARRELS = 26;
        private const int MIN_RUM_BARREL_VALUE = 10;
        private const int MAX_RUM_BARREL_VALUE = 20;
        private const int REWARD_RUM_BARREL_VALUE = 30;
        private const int MINE_VISIBILITY_RANGE = 5;
        private const int FIRE_DISTANCE_MAX = 10;
        private const int LOW_DAMAGE = 25;
        private const int HIGH_DAMAGE = 50;
        private const int MINE_DAMAGE = 25;
        private const int NEAR_MINE_DAMAGE = 10;

        private static readonly int MAX_SHIPS;
        private static readonly bool CANNONS_ENABLED;
        private static readonly bool MINES_ENABLED;
        private static readonly int MIN_MINES;
        private static readonly int MAX_MINES;
        private static readonly int MAX_SHIP_SPEED;

        static Referee()
        {
            switch(LEAGUE_LEVEL)
            {
                case 0: // 1 ship / no mines / speed 1
                    MAX_SHIPS = 1;
                    CANNONS_ENABLED = false;
                    MINES_ENABLED = false;
                    MIN_MINES = 0;
                    MAX_MINES = 0;
                    MAX_SHIP_SPEED = 2;
                    break;
                case 1: // add mines
                    MAX_SHIPS = 1;
                    CANNONS_ENABLED = true;
                    MINES_ENABLED = true;
                    MIN_MINES = 5;
                    MAX_MINES = 10;
                    MAX_SHIP_SPEED = 1;
                    break;
                case 2: // 3 ships max
                    MAX_SHIPS = 3;
                    CANNONS_ENABLED = true;
                    MINES_ENABLED = true;
                    MIN_MINES = 5;
                    MAX_MINES = 10;
                    MAX_SHIP_SPEED = 1;
                    break;
                default: // increase max speed
                    MAX_SHIPS = 3;
                    CANNONS_ENABLED = true;
                    MINES_ENABLED = true;
                    MIN_MINES = 5;
                    MAX_MINES = 10;
                    MAX_SHIP_SPEED = 2;
                    break;
            }
        }

        private static Regex PLAYER_INPUT_MOVE_PATTERN = new Regex("MOVE (?<x>-?[0-9]{1,8})\\s+(?<y>-?[0-9]{1,8})(?:\\s+(?<message>.+))?", RegexOptions.IgnoreCase);
        private static Regex PLAYER_INPUT_SLOWER_PATTERN = new Regex("SLOWER(?:\\s+(?<message>.+))?", RegexOptions.IgnoreCase);
        private static Regex PLAYER_INPUT_FASTER_PATTERN = new Regex("FASTER(?:\\s+(?<message>.+))?", RegexOptions.IgnoreCase);
        private static Regex PLAYER_INPUT_WAIT_PATTERN = new Regex("WAIT(?:\\s+(?<message>.+))?", RegexOptions.IgnoreCase);
        private static Regex PLAYER_INPUT_PORT_PATTERN = new Regex("PORT(?:\\s+(?<message>.+))?", RegexOptions.IgnoreCase);
        private static Regex PLAYER_INPUT_STARBOARD_PATTERN = new Regex("STARBOARD(?:\\s+(?<message>.+))?", RegexOptions.IgnoreCase);
        private static Regex PLAYER_INPUT_FIRE_PATTERN = new Regex("FIRE (?<x>-?[0-9]{1,8})\\s+(?<y>-?[0-9]{1,8})(?:\\s+(?<message>.+))?", RegexOptions.IgnoreCase);
        private static Regex PLAYER_INPUT_MINE_PATTERN = new Regex("MINE(?:\\s+(?<message>.+))?", RegexOptions.IgnoreCase);

        public static int clamp(int val, int min, int max)
        {
            return Math.Max(min, Math.Min(max, val));
        }

        static string join(params object[] col)
        {
            return string.Join(" ", col);
        }

        public class Coord
        {
            private readonly static int[,] DIRECTIONS_EVEN = new int[,] { { 1, 0 }, { 0, -1 }, { -1, -1 }, { -1, 0 }, { -1, 1 }, { 0, 1 } };
            private readonly static int[,] DIRECTIONS_ODD = new int[,] { { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, 0 }, { 0, 1 }, { 1, 1 } };
            public readonly int x;
            public readonly int y;

            public Coord(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public Coord(Coord other)
            {
                this.x = other.x;
                this.y = other.y;
            }

            public double angle(Coord targetPosition)
            {
                double dy = (targetPosition.y - this.y) * Math.Sqrt(3) / 2;
                double dx = targetPosition.x - this.x + ((this.y - targetPosition.y) & 1) * 0.5;
                double angle = -Math.Atan2(dy, dx) * 3 / Math.PI;
                if (angle < 0)
                {
                    angle += 6;
                }
                else if (angle >= 6)
                {
                    angle -= 6;
                }
                return angle;
            }

            public CubeCoordinate toCubeCoordinate()
            {
                int xp = x - (y - (y & 1)) / 2;
                int zp = y;
                int yp = -(xp + zp);
                return new CubeCoordinate(xp, yp, zp);
            }

            public Coord neighbor(int orientation)
            {
                int newY, newX;
                if (this.y % 2 == 1)
                {
                    newY = this.y + DIRECTIONS_ODD[orientation, 1];
                    newX = this.x + DIRECTIONS_ODD[orientation, 0];
                }
                else
                {
                    newY = this.y + DIRECTIONS_EVEN[orientation, 1];
                    newX = this.x + DIRECTIONS_EVEN[orientation, 0];
                }

                return new Coord(newX, newY);
            }

            public bool isInsideMap()
            {
                return x >= 0 && x < MAP_WIDTH && y >= 0 && y < MAP_HEIGHT;
            }

            public int distanceTo(Coord dst)
            {
                return this.toCubeCoordinate().distanceTo(dst.toCubeCoordinate());
            }

            public override int GetHashCode()
            {
                return x ^ y;
            }

            public override bool Equals(object obj)
            {
                var other = (Coord)obj;
                if (other == null)
                    return false;
                return y == other.y && x == other.x;
            }

            public override string ToString()
            {
                return join(x, y);
            }
        }

        public class CubeCoordinate
        {
            static int[,] directions = new int[,] { { 1, -1, 0 }, { +1, 0, -1 }, { 0, +1, -1 }, { -1, +1, 0 }, { -1, 0, +1 }, { 0, -1, +1 } };
            int x, y, z;

            public CubeCoordinate(int x, int y, int z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public Coord toOffsetCoordinate()
            {
                int newX = x + (z - (z & 1)) / 2;
                int newY = z;
                return new Coord(newX, newY);
            }

            public CubeCoordinate neighbor(int orientation)
            {
                int nx = this.x + directions[orientation, 0];
                int ny = this.y + directions[orientation, 1];
                int nz = this.z + directions[orientation, 2];

                return new CubeCoordinate(nx, ny, nz);
            }

            public int distanceTo(CubeCoordinate dst)
            {
                return (Math.Abs(x - dst.x) + Math.Abs(y - dst.y) + Math.Abs(z - dst.z)) / 2;
            }

            public String toString()
            {
                return join(x, y, z);
            }
        }

        public enum EntityType
        {
            SHIP,
            BARREL,
            MINE,
            CANNONBALL
        }

        public abstract class Entity
        {
            private static int UNIQUE_ENTITY_ID = 0;

            public int id;
            public EntityType type;
            public Coord position;

            public Entity(EntityType type, int x, int y)
            {
                this.id = UNIQUE_ENTITY_ID++;
                this.type = type;
                this.position = new Coord(x, y);
            }

            public virtual string toViewString()
            {
                return join(id, position.y, position.x);
            }

            public virtual string toPlayerString(int arg1, int arg2, int arg3, int arg4)
            {
                return join(id, type.ToString(), position.x, position.y, arg1, arg2, arg3, arg4);
            }
        }

        public class Mine : Entity
        {
            public Mine(int x, int y)
                : base(EntityType.MINE, x, y)
            {
            }

            public String toPlayerString(int playerIdx)
            {
                return toPlayerString(0, 0, 0, 0);
            }

            public List<Damage> explode(List<Ship> ships, bool force)
            {
                var damage = new List<Damage>();
                Ship victim = null;

                foreach (var ship in ships)
                {
                    if (position.Equals(ship.bow()) || position.Equals(ship.stern()) || position.Equals(ship.position))
                    {
                        damage.Add(new Damage(this.position, MINE_DAMAGE, true));
                        ship.damage(MINE_DAMAGE);
                        victim = ship;
                    }
                }

                if (force || victim != null)
                {
                    if (victim == null)
                    {
                        damage.Add(new Damage(this.position, MINE_DAMAGE, true));
                    }

                    foreach (var ship in ships)
                    {
                        if (ship != victim)
                        {
                            Coord impactPosition = null;
                            if (ship.stern().distanceTo(position) <= 1)
                            {
                                impactPosition = ship.stern();
                            }
                            if (ship.bow().distanceTo(position) <= 1)
                            {
                                impactPosition = ship.bow();
                            }
                            if (ship.position.distanceTo(position) <= 1)
                            {
                                impactPosition = ship.position;
                            }

                            if (impactPosition != null)
                            {
                                ship.damage(NEAR_MINE_DAMAGE);
                                damage.Add(new Damage(impactPosition, NEAR_MINE_DAMAGE, true));
                            }
                        }
                    }
                }

                return damage;
            }
        }

        public class Cannonball : Entity
        {
            public int ownerEntityId;
            public int srcX;
            public int srcY;
            public int initialRemainingTurns;
            public int remainingTurns;

            public Cannonball(int row, int col, int ownerEntityId, int srcX, int srcY, int remainingTurns)
                : base(EntityType.CANNONBALL, row, col)
            {
                this.ownerEntityId = ownerEntityId;
                this.srcX = srcX;
                this.srcY = srcY;
                this.initialRemainingTurns = this.remainingTurns = remainingTurns;
            }

            public override string toViewString()
            {
                return join(id, position.y, position.x, srcY, srcX, initialRemainingTurns, remainingTurns, ownerEntityId);
            }

            public string toPlayerString(int playerIdx)
            {
                return toPlayerString(ownerEntityId, remainingTurns, 0, 0);
            }
        }

        public class RumBarrel : Entity
        {
            public int health;

            public RumBarrel(int x, int y, int health)
                : base(EntityType.BARREL, x, y)
            {
                this.health = health;
            }

            public override string toViewString()
            {
                return join(id, position.y, position.x, health);
            }

            public string toPlayerString(int playerIdx)
            {
                return toPlayerString(health, 0, 0, 0);
            }
        }

        public class Damage
        {
            private Coord position;
            private int health;
            private bool hit;

            public Damage(Coord position, int health, bool hit)
            {
                this.position = position;
                this.health = health;
                this.hit = hit;
            }

            public String toViewString()
            {
                return join(position.y, position.x, health, (hit ? 1 : 0));
            }
        }

        public enum Action
        {
            FASTER,
            SLOWER,
            PORT,
            STARBOARD,
            FIRE,
            MINE
        }

        public class Ship : Entity
        {
            public int orientation;
            public int speed;
            public int health;
            public int initialHealth;
            public int owner;
            public String message;
            public Action? action;
            public int mineCooldown;
            public int cannonCooldown;
            public Coord target;
            public int newOrientation;
            public Coord newPosition;
            public Coord newBowCoordinate;
            public Coord newSternCoordinate;

            public Ship(int x, int y, int orientation, int owner)
                : base(EntityType.SHIP, x, y)
            {
                this.orientation = orientation;
                this.speed = 0;
                this.health = INITIAL_SHIP_HEALTH;
                this.owner = owner;
            }

            public override string toViewString()
            {
                return join(id, position.y, position.x, orientation, health, speed, (action != null ? action.ToString() : "WAIT"), bow().y, bow().x, stern().y,
                        stern().x, " ;" + (message != null ? message : ""));
            }

            public String toPlayerString(int playerIdx)
            {
                return toPlayerString(orientation, speed, health, owner == playerIdx ? 1 : 0);
            }

            public void setMessage(string message)
            {
                if (message != null && message.Length > 50)
                {
                    message = message.Substring(0, 50) + "...";
                }
                this.message = message;
            }

            public void moveTo(int x, int y)
            {
                Coord currentPosition = this.position;
                Coord targetPosition = new Coord(x, y);

                if (currentPosition.Equals(targetPosition))
                {
                    this.action = Action.SLOWER;
                    return;
                }

                double targetAngle, angleStraight, anglePort, angleStarboard, centerAngle, anglePortCenter, angleStarboardCenter;

                switch (speed)
                {
                    case 2:
                        this.action = Action.SLOWER;
                        break;
                    case 1:
                        // Suppose we've moved first
                        currentPosition = currentPosition.neighbor(orientation);
                        if (!currentPosition.isInsideMap())
                        {
                            this.action = Action.SLOWER;
                            break;
                        }

                        // Target reached at next turn
                        if (currentPosition.Equals(targetPosition))
                        {
                            this.action = null;
                            break;
                        }

                        // For each neighbor cell, find the closest to target
                        targetAngle = currentPosition.angle(targetPosition);
                        angleStraight = Math.Min(Math.Abs(orientation - targetAngle), 6 - Math.Abs(orientation - targetAngle));
                        anglePort = Math.Min(Math.Abs((orientation + 1) - targetAngle), Math.Abs((orientation - 5) - targetAngle));
                        angleStarboard = Math.Min(Math.Abs((orientation + 5) - targetAngle), Math.Abs((orientation - 1) - targetAngle));

                        centerAngle = currentPosition.angle(new Coord(MAP_WIDTH / 2, MAP_HEIGHT / 2));
                        anglePortCenter = Math.Min(Math.Abs((orientation + 1) - centerAngle), Math.Abs((orientation - 5) - centerAngle));
                        angleStarboardCenter = Math.Min(Math.Abs((orientation + 5) - centerAngle), Math.Abs((orientation - 1) - centerAngle));

                        // Next to target with bad angle, slow down then rotate (avoid to turn around the target!)
                        if (currentPosition.distanceTo(targetPosition) == 1 && angleStraight > 1.5)
                        {
                            this.action = Action.SLOWER;
                            break;
                        }

                        int? distanceMin = null;

                        // Test forward
                        Coord nextPosition = currentPosition.neighbor(orientation);
                        if (nextPosition.isInsideMap())
                        {
                            distanceMin = nextPosition.distanceTo(targetPosition);
                            this.action = null;
                        }

                        // Test port
                        nextPosition = currentPosition.neighbor((orientation + 1) % 6);
                        if (nextPosition.isInsideMap())
                        {
                            int distance = nextPosition.distanceTo(targetPosition);
                            if (distanceMin == null || distance < distanceMin || distance == distanceMin && anglePort < angleStraight - 0.5)
                            {
                                distanceMin = distance;
                                this.action = Action.PORT;
                            }
                        }

                        // Test starboard
                        nextPosition = currentPosition.neighbor((orientation + 5) % 6);
                        if (nextPosition.isInsideMap())
                        {
                            int distance = nextPosition.distanceTo(targetPosition);
                            if (distanceMin == null || distance < distanceMin
                                    || (distance == distanceMin && angleStarboard < anglePort - 0.5 && this.action == Action.PORT)
                                    || (distance == distanceMin && angleStarboard < angleStraight - 0.5 && this.action == null)
                                    || (distance == distanceMin && this.action == Action.PORT && angleStarboard == anglePort
                                            && angleStarboardCenter < anglePortCenter)
                                    || (distance == distanceMin && this.action == Action.PORT && angleStarboard == anglePort
                                            && angleStarboardCenter == anglePortCenter && (orientation == 1 || orientation == 4)))
                            {
                                distanceMin = distance;
                                this.action = Action.STARBOARD;
                            }
                        }
                        break;
                    case 0:
                        // Rotate ship towards target
                        targetAngle = currentPosition.angle(targetPosition);
                        angleStraight = Math.Min(Math.Abs(orientation - targetAngle), 6 - Math.Abs(orientation - targetAngle));
                        anglePort = Math.Min(Math.Abs((orientation + 1) - targetAngle), Math.Abs((orientation - 5) - targetAngle));
                        angleStarboard = Math.Min(Math.Abs((orientation + 5) - targetAngle), Math.Abs((orientation - 1) - targetAngle));

                        centerAngle = currentPosition.angle(new Coord(MAP_WIDTH / 2, MAP_HEIGHT / 2));
                        anglePortCenter = Math.Min(Math.Abs((orientation + 1) - centerAngle), Math.Abs((orientation - 5) - centerAngle));
                        angleStarboardCenter = Math.Min(Math.Abs((orientation + 5) - centerAngle), Math.Abs((orientation - 1) - centerAngle));

                        Coord forwardPosition = currentPosition.neighbor(orientation);

                        this.action = null;

                        if (anglePort <= angleStarboard)
                        {
                            this.action = Action.PORT;
                        }

                        if (angleStarboard < anglePort || angleStarboard == anglePort && angleStarboardCenter < anglePortCenter
                                || angleStarboard == anglePort && angleStarboardCenter == anglePortCenter && (orientation == 1 || orientation == 4))
                        {
                            this.action = Action.STARBOARD;
                        }

                        if (forwardPosition.isInsideMap() && angleStraight <= anglePort && angleStraight <= angleStarboard)
                        {
                            this.action = Action.FASTER;
                        }
                        break;
                }

            }

            public void faster()
            {
                this.action = Action.FASTER;
            }

            public void slower()
            {
                this.action = Action.SLOWER;
            }

            public void port()
            {
                this.action = Action.PORT;
            }

            public void starboard()
            {
                this.action = Action.STARBOARD;
            }

            public void placeMine()
            {
                if (MINES_ENABLED)
                {
                    this.action = Action.MINE;
                }
            }

            public Coord stern()
            {
                return position.neighbor((orientation + 3) % 6);
            }

            public Coord bow()
            {
                return position.neighbor(orientation);
            }

            public Coord newStern()
            {
                return position.neighbor((newOrientation + 3) % 6);
            }

            public Coord newBow()
            {
                return position.neighbor(newOrientation);
            }

            public bool at(Coord coord)
            {
                Coord _stern = stern();
                Coord _bow = bow();
                return _stern != null && _stern.Equals(coord) || _bow != null && _bow.Equals(coord) || position.Equals(coord);
            }

            public bool newBowIntersect(Ship other)
            {
                return newBowCoordinate != null && (newBowCoordinate.Equals(other.newBowCoordinate) || newBowCoordinate.Equals(other.newPosition)
                        || newBowCoordinate.Equals(other.newSternCoordinate));
            }

            public bool newBowIntersect(List<Ship> ships)
            {
                foreach (var other in ships)
                {
                    if (this != other && newBowIntersect(other))
                    {
                        return true;
                    }
                }
                return false;
            }

            public bool newPositionsIntersect(Ship other)
            {
                bool sternCollision = newSternCoordinate != null && (newSternCoordinate.Equals(other.newBowCoordinate)
                        || newSternCoordinate.Equals(other.newPosition) || newSternCoordinate.Equals(other.newSternCoordinate));
                bool centerCollision = newPosition != null && (newPosition.Equals(other.newBowCoordinate) || newPosition.Equals(other.newPosition)
                        || newPosition.Equals(other.newSternCoordinate));
                return newBowIntersect(other) || sternCollision || centerCollision;
            }

            public bool newPositionsIntersect(List<Ship> ships)
            {
                foreach (var other in ships)
                {
                    if (this != other && newPositionsIntersect(other))
                    {
                        return true;
                    }
                }
                return false;
            }

            public void damage(int health)
            {
                this.health -= health;
                if (this.health <= 0)
                {
                    this.health = 0;
                }
            }

            public void heal(int health)
            {
                this.health += health;
                if (this.health > MAX_SHIP_HEALTH)
                {
                    this.health = MAX_SHIP_HEALTH;
                }
            }

            public void fire(int x, int y)
            {
                if (CANNONS_ENABLED)
                {
                    Coord target = new Coord(x, y);
                    this.target = target;
                    this.action = Action.FIRE;
                }
            }
        }

        private class Player
        {
            public int id;
            public List<Ship> ships;
            public List<Ship> shipsAlive;

            public Player(int id)
            {
                this.id = id;
                this.ships = new List<Ship>();
                this.shipsAlive = new List<Ship>();
            }

            public void setDead()
            {
                foreach (var ship in ships)
                {
                    ship.health = 0;
                }
            }

            public int getScore()
            {
                int score = 0;
                foreach (var ship in ships)
                {
                    score += ship.health;
                }
                return score;
            }

            public List<String> toViewString()
            {
                var data = new List<string>();

                data.Add(id.ToString());
                foreach (var ship in ships)
                {
                    data.Add(ship.toViewString());
                }

                return data;
            }
        }

        private long seed;
        private List<Cannonball> cannonballs = new List<Cannonball>();
        private List<Mine> mines = new List<Mine>();
        private List<RumBarrel> barrels = new List<RumBarrel>();
        private List<Player> players = new List<Player>();
        private List<Ship> ships;
        private List<Damage> damage = new List<Damage>();
        private List<Coord> cannonBallExplosions = new List<Coord>();
        private int shipsPerPlayer;
        private int mineCount;
        private int barrelCount;
        private Random random;

        public Referee(Stream sin, Stream sout, Stream err)
        {

        }

        public class Random // Beresta added: Java Random port from stackoverflow: http://stackoverflow.com/questions/2147524/c-java-number-randomization
        {
            public Random(UInt64 seed)
            {
                this.seed = (seed ^ 0x5DEECE66DUL) & ((1UL << 48) - 1);
            }

            public int nextInt(int n)
            {
                if (n <= 0) throw new ArgumentException("n must be positive");

                if ((n & -n) == n)  // i.e., n is a power of 2
                    return (int)((n * (long)Next(31)) >> 31);

                long bits, val;
                do
                {
                    bits = Next(31);
                    val = bits % (UInt32)n;
                }
                while (bits - val + (n - 1) < 0);

                return (int)val;
            }

            public long nextLong()
            {
                return ((long)Next(32) << 32) + Next(32);
            }

            protected UInt32 Next(int bits)
            {
                seed = (seed * 0x5DEECE66DL + 0xBL) & ((1L << 48) - 1);

                return (UInt32)(seed >> (48 - bits));
            }

            private UInt64 seed;
        }

        public static long parseProperty(Dictionary<string, string> prop, string key, long dft)
        {
            if (!prop.ContainsKey(key))
            {
                return dft;
            }
            else
            {
                var val = prop[key];
                try
                {
                    return long.Parse(val);
                }
                catch
                {
                    return dft;
                }
            }
        }

        public void initReferee(int playerCount, Dictionary<string, string> prop)
        {
            seed = parseProperty(prop, "seed", new Random((ulong)DateTime.Now.Ticks).nextLong());
            random = new Random((ulong)seed);

            shipsPerPlayer = clamp((int)parseProperty(prop, "shipsPerPlayer", (random.nextInt(1 + MAX_SHIPS - MIN_SHIPS) + MIN_SHIPS)), MIN_SHIPS, MAX_SHIPS);

            if (MAX_MINES > MIN_MINES)
            {
                mineCount = clamp((int)parseProperty(prop, "mineCount", random.nextInt(MAX_MINES - MIN_MINES) + MIN_MINES), MIN_MINES, MAX_MINES);
            }
            else
            {
                mineCount = MIN_MINES;
            }

            barrelCount = clamp((int)parseProperty(prop, "barrelCount", random.nextInt(MAX_RUM_BARRELS - MIN_RUM_BARRELS) + MIN_RUM_BARRELS), MIN_RUM_BARRELS, MAX_RUM_BARRELS);

            // Generate Players
            for (int i = 0; i < playerCount; i++)
            {
                players.Add(new Player(i));
            }
            // Generate Ships
            for (int j = 0; j < shipsPerPlayer; j++)
            {
                int xMin = 1 + j * MAP_WIDTH / shipsPerPlayer;
                int xMax = (j + 1) * MAP_WIDTH / shipsPerPlayer - 2;

                int y = 1 + random.nextInt(MAP_HEIGHT / 2 - 2);
                int x = xMin + random.nextInt(1 + xMax - xMin);
                int orientation = random.nextInt(6);

                Ship ship0 = new Ship(x, y, orientation, 0);
                Ship ship1 = new Ship(x, MAP_HEIGHT - 1 - y, (6 - orientation) % 6, 1);

                players[0].ships.Add(ship0);
                players[1].ships.Add(ship1);
                players[0].shipsAlive.Add(ship0);
                players[1].shipsAlive.Add(ship1);
            }

            ships = players.SelectMany(p => p.ships).ToList();

            // Generate mines
            while (mines.Count < mineCount)
            {
                int x = 1 + random.nextInt(MAP_WIDTH - 2);
                int y = 1 + random.nextInt(MAP_HEIGHT / 2);

                Mine m = new Mine(x, y);
                bool cellIsFreeOfMines = !mines.Any(m1 => m1.position.Equals(m.position));
                bool cellIsFreeOfShips = !ships.Any(s => s.position.Equals(m.position));
                if (cellIsFreeOfMines && cellIsFreeOfShips)
                {
                    if (y != MAP_HEIGHT - 1 - y)
                    {
                        mines.Add(new Mine(x, MAP_HEIGHT - 1 - y));
                    }
                    mines.Add(m);
                }
            }
            mineCount = mines.Count;

            // Generate supplies
            while (barrels.Count < barrelCount)
            {
                int x = 1 + random.nextInt(MAP_WIDTH - 2);
                int y = 1 + random.nextInt(MAP_HEIGHT / 2);
                int h = MIN_RUM_BARREL_VALUE + random.nextInt(1 + MAX_RUM_BARREL_VALUE - MIN_RUM_BARREL_VALUE);

                RumBarrel m = new RumBarrel(x, y, h);
                bool cellIsFreeOfBarrels = barrels.Any(b => b.position.Equals(m.position));
                bool cellIsFreeOfMines = !mines.Any(m1 => m1.position.Equals(m.position));
                bool cellIsFreeOfShips = !ships.Any(s => s.position.Equals(m.position));
                if (cellIsFreeOfShips && cellIsFreeOfMines && cellIsFreeOfBarrels)
                {
                    if (y != MAP_HEIGHT - 1 - y)
                    {
                        barrels.Add(new RumBarrel(x, MAP_HEIGHT - 1 - y, h));
                    }
                    barrels.Add(m);
                }
            }
            barrelCount = barrels.Count;
        }

        protected Dictionary<string, string> getConfiguration()
        {
            var prop = new Dictionary<string, string>();
            prop.Add("seed", seed.ToString());
            prop.Add("shipsPerPlayer", shipsPerPlayer.ToString());
            prop.Add("barrelCount", barrelCount.ToString());
            prop.Add("mineCount", mineCount.ToString());
            return prop;
        }

        public void prepare(int round)
        {
            foreach (var player in players)
            {
                foreach (var ship in player.ships)
                {
                    ship.action = null;
                    ship.message = null;
                }
            }
            cannonBallExplosions.Clear();
            damage.Clear();
        }

        protected int getExpectedOutputLineCountForPlayer(int playerIdx)
        {
            return this.players[playerIdx].shipsAlive.Count;
        }

        protected void handlePlayerOutput(int frame, int round, int playerIdx, String[] outputs)
        {
            Player player = players[playerIdx];

            try
            {
                int i = 0;
                foreach (var line in outputs)
                {
                    var matchWait = PLAYER_INPUT_WAIT_PATTERN.Match(line);
                    var matchMove = PLAYER_INPUT_MOVE_PATTERN.Match(line);
                    var matchFaster = PLAYER_INPUT_FASTER_PATTERN.Match(line);
                    var matchSlower = PLAYER_INPUT_SLOWER_PATTERN.Match(line);
                    var matchPort = PLAYER_INPUT_PORT_PATTERN.Match(line);
                    var matchStarboard = PLAYER_INPUT_STARBOARD_PATTERN.Match(line);
                    var matchFire = PLAYER_INPUT_FIRE_PATTERN.Match(line);
                    var matchMine = PLAYER_INPUT_MINE_PATTERN.Match(line);
                    Ship ship = player.shipsAlive[i++];

                    if (matchMove.Success)
                    {
                        int x = int.Parse(matchMove.Groups["x"].Value);
                        int y = int.Parse(matchMove.Groups["y"].Value);
                        ship.setMessage(matchMove.Groups["message"].Value);
                        ship.moveTo(x, y);
                    }
                    else if (matchFaster.Success)
                    {
                        ship.setMessage(matchFaster.Groups["message"].Value);
                        ship.faster();
                    }
                    else if (matchSlower.Success)
                    {
                        ship.setMessage(matchSlower.Groups["message"].Value);
                        ship.slower();
                    }
                    else if (matchPort.Success)
                    {
                        ship.setMessage(matchPort.Groups["message"].Value);
                        ship.port();
                    }
                    else if (matchStarboard.Success)
                    {
                        ship.setMessage(matchStarboard.Groups["message"].Value);
                        ship.starboard();
                    }
                    else if (matchWait.Success)
                    {
                        ship.setMessage(matchWait.Groups["message"].Value);
                    }
                    else if (matchMine.Success)
                    {
                        ship.setMessage(matchMine.Groups["message"].Value);
                        ship.placeMine();
                    }
                    else if (matchFire.Success)
                    {
                        int x = int.Parse(matchFire.Groups["x"].Value);
                        int y = int.Parse(matchFire.Groups["y"].Value);
                        ship.setMessage(matchFire.Groups["message"].Value);
                        ship.fire(x, y);
                    }
                    else
                    {
                        throw new ArgumentException("A valid action", line);
                    }
                }
            }
            catch
            {
                player.setDead();
                throw;
            }
        }

        private void decrementRum()
        {
            foreach (var ship in ships)
            {
                ship.damage(1);
            }
        }

        private void updateInitialRum()
        {
            foreach(var ship in ships)
            {
                ship.initialHealth = ship.health;
            }
        }

        private void moveCannonballs()
        {
            cannonballs.RemoveAll(b => b.remainingTurns == 0);

            foreach (var ball in cannonballs)
            {
                ball.remainingTurns--;

                if (ball.remainingTurns == 0)
                {
                    cannonBallExplosions.Add(ball.position);
                }
            }
        }

        private void applyActions()
        {
            foreach (var player in players)
            {
                foreach (var ship in player.shipsAlive)
                {
                    if (ship.mineCooldown > 0)
                    {
                        ship.mineCooldown--;
                    }
                    if (ship.cannonCooldown > 0)
                    {
                        ship.cannonCooldown--;
                    }

                    ship.newOrientation = ship.orientation;

                    if (ship.action != null)
                    {
                        switch (ship.action.Value)
                        {
                            case Action.FASTER:
                                if (ship.speed < MAX_SHIP_SPEED)
                                {
                                    ship.speed++;
                                }
                                break;
                            case Action.SLOWER:
                                if (ship.speed > 0)
                                {
                                    ship.speed--;
                                }
                                break;
                            case Action.PORT:
                                ship.newOrientation = (ship.orientation + 1) % 6;
                                break;
                            case Action.STARBOARD:
                                ship.newOrientation = (ship.orientation + 5) % 6;
                                break;
                            case Action.MINE:
                                if (ship.mineCooldown == 0)
                                {
                                    Coord target = ship.stern().neighbor((ship.orientation + 3) % 6);

                                    if (target.isInsideMap())
                                    {
                                        bool cellIsFreeOfBarrels = !barrels.Any(b => b.position.Equals(target));
                                        bool cellIsFreeOfMines = !mines.Any(m => m.position.Equals(target));
                                        bool cellIsFreeOfShips = !ships.Any(s => s != ship && s.at(target));

                                        if (cellIsFreeOfBarrels && cellIsFreeOfShips && cellIsFreeOfMines)
                                        {
                                            ship.mineCooldown = COOLDOWN_MINE;
                                            Mine mine = new Mine(target.x, target.y);
                                            mines.Add(mine);
                                        }
                                    }

                                }
                                break;
                            case Action.FIRE:
                                int distance = ship.bow().distanceTo(ship.target);
                                if (ship.target.isInsideMap() && distance <= FIRE_DISTANCE_MAX && ship.cannonCooldown == 0)
                                {
                                    int travelTime = (int)(1 + Math.Round(ship.bow().distanceTo(ship.target) / 3.0));
                                    cannonballs.Add(new Cannonball(ship.target.x, ship.target.y, ship.id, ship.bow().x, ship.bow().y, travelTime));
                                    ship.cannonCooldown = COOLDOWN_CANNON;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        private void checkBarrelCollisions(Ship ship)
        {
            Coord bow = ship.bow();
            Coord stern = ship.stern();
            Coord center = ship.position;

            var collisionBarrels = barrels.Where(barrel => barrel.position.Equals(bow) || barrel.position.Equals(stern) || barrel.position.Equals(center)).ToArray();
            foreach (var barrel in collisionBarrels)
            {
                ship.heal(barrel.health);
                barrels.Remove(barrel);
            }
        }

        private void checkMineCollisions()
        {
            var collisionMines = mines.Select(mine => Tuple.Create(mine, mine.explode(ships, false))).Where(m => m.Item2.Count > 0).ToArray();
            foreach (var mine in collisionMines)
            {
                damage.AddRange(mine.Item2);
                mines.Remove(mine.Item1);
            }
        }

        private void checkCollisions()
        {
            // Check collisions with Barrels
            foreach(var ship in ships)
            {
                checkBarrelCollisions(ship);
            }

            // Check collisions with Mines
            checkMineCollisions();
        }

        private void moveShips() {
        	// ---
        	// Go forward
        	// ---
        	for (int i = 1; i <= MAX_SHIP_SPEED; i++) {
        		foreach (var player in players) {
        			foreach (var ship in player.shipsAlive) {
        				ship.newPosition = ship.position;
        				ship.newBowCoordinate = ship.bow();
        				ship.newSternCoordinate = ship.stern();

        				if (i > ship.speed) {
        					continue;
        				}

        				Coord newCoordinate = ship.position.neighbor(ship.orientation);

        				if (newCoordinate.isInsideMap()) {
        					// Set new coordinate.
        					ship.newPosition = newCoordinate;
        					ship.newBowCoordinate = newCoordinate.neighbor(ship.orientation);
        					ship.newSternCoordinate = newCoordinate.neighbor((ship.orientation + 3) % 6);
        				} else {
        					// Stop ship!
        					ship.speed = 0;
        				}
        			}
        		}

        		// Check ship and obstacles collisions
        		var collisions = new List<Ship>();
        		bool collisionDetected = true;
        		while (collisionDetected) {
        			collisionDetected = false;

        			foreach (Ship ship in ships) {
        				if (ship.newBowIntersect(ships)) {
        					collisions.Add(ship);
        				}
        			}

        			foreach (Ship ship in collisions) {
        				// Revert last move
        				ship.newPosition = ship.position;
        				ship.newBowCoordinate = ship.bow();
        				ship.newSternCoordinate = ship.stern();

        				// Stop ships
        				ship.speed = 0;

        				collisionDetected = true;
        			}
        			collisions.Clear();
        		}

                // Move ships to their new location
                foreach (var ship in ships)
                {
                    ship.position = ship.newPosition;
                }

                checkCollisions();
            }
        }

        private void rotateShips() {
            // Rotate
            foreach (var player in players)
            {
                foreach (var ship in player.shipsAlive)
                {
                    ship.newPosition = ship.position;
        			ship.newBowCoordinate = ship.newBow();
        			ship.newSternCoordinate = ship.newStern();
        		}
        	}

        	// Check collisions
        	bool collisionDetected = true;
            var collisions = new List<Ship>();
            while (collisionDetected) {
        		collisionDetected = false;

        		foreach (var ship in ships) {
        			if (ship.newPositionsIntersect(ships)) {
        				collisions.Add(ship);
        			}
        		}

                foreach (var ship in collisions) {
        			ship.newOrientation = ship.orientation;
        			ship.newBowCoordinate = ship.newBow();
        			ship.newSternCoordinate = ship.newStern();
        			ship.speed = 0;
        			collisionDetected = true;
        		}

        		collisions.Clear();
        	}

            // Apply rotation
            foreach (var ship in ships)
            {
                ship.orientation = ship.newOrientation;
            }

            checkCollisions();
        }

        private bool gameIsOver() {
        	foreach (var player in players) {
        		if (player.shipsAlive.Count == 0) {
        			return true;
        		}
        	}
        	return barrels.Count == 0 && LEAGUE_LEVEL == 0;
        }

        void explodeShips() {
            for (var i = cannonBallExplosions.Count - 1; i >= 0; i--) {
                var position = cannonBallExplosions[i];
                foreach (var ship in ships)
                {
                    if (position.Equals(ship.bow()) || position.Equals(ship.stern()))
                    {
                        damage.Add(new Damage(position, LOW_DAMAGE, true));
                        ship.damage(LOW_DAMAGE);
                        cannonBallExplosions.RemoveAt(i);
                        break;
                    }
                    else if (position.Equals(ship.position))
                    {
                        damage.Add(new Damage(position, HIGH_DAMAGE, true));
                        ship.damage(HIGH_DAMAGE);
                        cannonBallExplosions.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        void explodeMines() {
            for (var i = cannonBallExplosions.Count - 1; i >= 0; i--)
            {
                var position = cannonBallExplosions[i];
                for(var j = mines.Count - 1; j >= 0; j--)
                {
                    var mine = mines[j];
                    if (mine.position.Equals(position))
                    {
                        damage.AddRange(mine.explode(ships, true));
                        cannonBallExplosions.RemoveAt(i);
                        mines.RemoveAt(j);
                        break;
                    }
                }
            }
        }

        void explodeBarrels() {
            for (var i = cannonBallExplosions.Count - 1; i >= 0; i--)
            {
                var position = cannonBallExplosions[i];
                for (var j = barrels.Count - 1; j >= 0; j--)
                {
                    var barrel = barrels[j];
                    if (barrel.position.Equals(position))
                    {
                        damage.Add(new Damage(position, 0, true));
                        cannonBallExplosions.RemoveAt(i);
                        barrels.RemoveAt(j);
                        break;
                    }
                }
            }
        }

        protected void updateGame(int round) {
        	moveCannonballs();
        	decrementRum();
            updateInitialRum();

        	applyActions();
        	moveShips();
        	rotateShips();

        	explodeShips();
        	explodeMines();
        	explodeBarrels();

            // For each sunk ship, create a new rum barrel with the amount of rum the ship had at the begin of the turn (up to 30).
            foreach(var ship in ships)
            {
                if(ship.health <= 0)
                {
                    int reward = Math.Min(REWARD_RUM_BARREL_VALUE, ship.initialHealth);
                    if(reward > 0)
                    {
                        barrels.Add(new RumBarrel(ship.position.x, ship.position.y, reward));
                    }
                }
            }

            foreach (var position in cannonBallExplosions) {
        		damage.Add(new Damage(position, 0, false));
        	}

            for(var i = ships.Count - 1; i >= 0; i--)
            {
                Ship ship = ships[i];
                if (ship.health <= 0)
                {
                    players[ship.owner].shipsAlive.Remove(ship);
                    ships.RemoveAt(i);
                }
            }

        	if (gameIsOver()) {
        		throw new Exception("endReached");
        	}
        }

        protected void populateMessages(Dictionary<string, string> prop) {
            prop.Add("endReached", "End reached");
        }

        protected String[] getInitInputForPlayer(int playerIdx) {
        	return new String[0];
        }

        protected String[] getInputForPlayer(int round, int playerIdx) {
        	var data = new List<string>();

        	// Player's ships first
        	foreach (var ship in players[playerIdx].shipsAlive) {
        		data.Add(ship.toPlayerString(playerIdx));
        	}

        	// Number of ships
        	data.Insert(0, data.Count.ToString());

        	// Opponent's ships
        	foreach (var ship in players[(playerIdx + 1) % 2].shipsAlive) {
        		data.Add(ship.toPlayerString(playerIdx));
        	}

        	// Visible mines
        	foreach (var mine in mines) {
        		bool visible = false;
        		foreach (var ship in players[playerIdx].shipsAlive) {
        			if (ship.position.distanceTo(mine.position) <= MINE_VISIBILITY_RANGE) {
        				visible = true;
        				break;
        			}
        		}
        		if (visible) {
        			data.Add(mine.toPlayerString(playerIdx));
        		}
        	}

        	foreach (var ball in cannonballs) {
        		data.Add(ball.toPlayerString(playerIdx));
        	}

            foreach (var barrel in barrels) {
        		data.Add(barrel.toPlayerString(playerIdx));
        	}

        	data.Insert(1, (data.Count - 1).ToString());

            return data.ToArray();
        }

        protected String[] getInitDataForView() {
        	var data = new List<string>();

        	data.Add(join(MAP_WIDTH, MAP_HEIGHT, players[0].ships.Count, MINE_VISIBILITY_RANGE));

        	data.Insert(0, (data.Count + 1).ToString());

            return data.ToArray();
        }

        protected String[] getFrameDataForView(int round, int frame, bool keyFrame) {
        	var data = new List<string>();

        	foreach (var player in players) {
        		data.AddRange(player.toViewString());
        	}
        	data.Add(cannonballs.Count.ToString());
            foreach (Cannonball ball in cannonballs) {
        		data.Add(ball.toViewString());
        	}
        	data.Add(mines.Count.ToString());
            foreach (var mine in mines) {
        		data.Add(mine.toViewString());
        	}
        	data.Add(barrels.Count.ToString());
            foreach (var barrel in barrels) {
        		data.Add(barrel.toViewString());
        	}
        	data.Add(damage.Count.ToString());
            foreach (var d in damage) {
        		data.Add(d.toViewString());
        	}

        	return data.ToArray();
        }

        protected String getGameName() {
        	return "CodersOfTheCaribbean";
        }

        protected String getHeadlineAtGameStartForConsole() {
        	return null;
        }

        protected int getMinimumPlayerCount() {
        	return 2;
        }

        protected bool showTooltips() {
        	return true;
        }

        protected String[] getPlayerActions(int playerIdx, int round) {
        	return new String[0];
        }

        protected bool isPlayerDead(int playerIdx) {
        	return false;
        }

        protected String getDeathReason(int playerIdx) {
        	return "$" + playerIdx + ": Eliminated!";
        }

        protected int getScore(int playerIdx) {
        	return players[playerIdx].getScore();
        }

        protected String[] getGameSummary(int round) {
        	return new String[0];
        }

        protected void setPlayerTimeout(int frame, int round, int playerIdx) {
        	players[playerIdx].setDead();
        }

        protected int getMaxRoundCount(int playerCount) {
        	return 200;
        }

        protected int getMillisTimeForRound() {
        	return 50;
        }

    }

}