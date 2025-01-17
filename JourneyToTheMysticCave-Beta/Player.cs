﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace JourneyToTheMysticCave_Beta
{
    internal class Player : GameEntity
    {
        int dirX;
        int dirY;
        bool inDeep = false;
        int moveCount;
        public Point2D pos { get; set; }

        public bool attackedEnemy = false;
        public bool itemPickedUp = false;
        private Enemy lastEncountered;
        public int money;

        public Enemy GetLastEnountered()
        {
            return lastEncountered;
        }

        Map map;
        GameStats gameStats;
        EnemyManager enemyManager;
        LegendColors legendColors;
        LevelManager levelManager;
        ItemManager itemManager;
        ShopManager shopManager;
        List<Shop> _shops;
        Gamelog gamelog;


        public Player()
        {
            healthSystem = new HealthSystem();
        }

        public void Init(Map map, GameStats gameStats, LegendColors legendColors, EnemyManager enemyManager, LevelManager levelManager, ItemManager itemManager, List<Shop> shops, Gamelog gamelog)
        {
            this.map = map;
            this.gameStats = gameStats;
            this.legendColors = legendColors;
            this.enemyManager = enemyManager;
            this.levelManager = levelManager;
            this.itemManager = itemManager;
            this._shops = shops;
            this.shopManager = new ShopManager();
            this.gamelog = gamelog;
            shopManager.Init(gameStats, legendColors, this, map);

            healthSystem.health = gameStats.PlayerHealth;
            character = gameStats.PlayerCharacter;
            pos = gameStats.PlayerPos;
            damage = gameStats.PlayerDamage;
            name = gameStats.PlayerName;
            money = gameStats.MoneyCount;
        }

        public void Update()
        {
            Movement();
        }

        public void Draw()
        {
            if (pos.Equals(default(Point2D)) || healthSystem == null || map == null || legendColors == null)
            {
                throw new NullReferenceException("Player position, health system, map, or legend colors are not initialized.");
            }

            Console.SetCursorPosition(pos.x, pos.y);

            legendColors.MapColor(character);
            if (map.GetCurrentMapContent()[pos.y, pos.x] == 'P')
                Console.BackgroundColor = ConsoleColor.DarkGreen;
            else if (map.GetCurrentMapContent()[pos.y, pos.x] == '~')
                Console.BackgroundColor = ConsoleColor.Blue;

            Console.Write(character);
            Console.ResetColor();
            Console.CursorVisible = false;
        }

        private void Movement() // Player Movement
        {
            if (!healthSystem.mapDead) // If player is alive
            {
                PlayerInput(); // Get player input

                int newX = pos.x + dirX; // New X position
                int newY = pos.y + dirY; // New Y position

                if (CheckBoundaries(newX, newY)) // Check if player is within boundaries
                {
                    lastEncountered = GetEnemyAtPosition(newX, newY); // Get enemy at new position
                    if (lastEncountered != null) // If enemy is at new position
                        AttackEnemy(lastEncountered); // Attack enemy
                    if(!attackedEnemy) // If player didn't attack enemy
                    {
                        CheckFloor(newX, newY); // Check if player is on floor
                        CheckShop(newX, newY); // Check if player is on shop
                    }
                    attackedEnemy = false; // Reset attacked enemy
                }
            }
        }

        private void PlayerInput()
        {
            ConsoleKeyInfo input = Console.ReadKey(true); // Read key without displaying it

            dirX = 0;
            dirY = 0;

            switch (input.Key)
            {
                case ConsoleKey.W: dirY = -1; break;
                case ConsoleKey.S: dirY = 1; break;
                case ConsoleKey.A: dirX = -1; break;
                case ConsoleKey.D: dirX = 1; break;
                case ConsoleKey.Q: dirY = -1; dirX = -1; break;
                case ConsoleKey.E: dirY = -1; dirX = 1; break;
                case ConsoleKey.Z: dirY = 1; dirX = -1; break;
                case ConsoleKey.C: dirY = 1; dirX = 1; break;
                case ConsoleKey.Spacebar: return; // using for testing, player doesn't move
                case ConsoleKey.Escape: System.Environment.Exit(0); return;
            }
        }

        private Enemy GetEnemyAtPosition(int x, int y)
        {
            foreach (Enemy enemy in enemyManager.enemies)
            {
                if (enemy.pos.x == x && enemy.pos.y == y)
                {
                    if ((enemy is Ranger && levelManager.mapLevel == 0) ||
                        (enemy is Mage && levelManager.mapLevel == 1) ||
                        (enemy is Melee && levelManager.mapLevel == 2) ||
                        (enemy is Boss && enemyManager.AreAllMeleeDead()))
                    {
                        return enemy;
                    }
                }
            }
            return null;
        }

        private void CheckShop(int x, int y)
        {
            foreach (var shop in shopManager.GetShops())
            {
                if (shop.pos.x == x && shop.pos.y == y)
                {
                    shop.Interact(this, gamelog, gameStats, levelManager);
                }
            }
        }

        private void AttackEnemy(Enemy enemy)
        {
            enemy.healthSystem.TakeDamage(damage, "Attacked");
            attackedEnemy = true;
        }

        private bool CheckBoundaries(int x, int y)
        {
            return map.GetCurrentMapContent()[y, x] != '#' && map.GetCurrentMapContent()[y, x] != '^';
        }

        private void CheckFloor(int x, int y)
        {
            if (map.GetCurrentMapContent()[y, x] == '~' && inDeep == false)
            {
                inDeep = true;
                pos = new Point2D { x = x, y = y };
                moveCount = 0;
            }
            else if (map.GetCurrentMapContent()[y, x] == 'P')
                healthSystem.TakeDamage(gameStats.PoisonDamage, "Floor");

            if(!inDeep)
                pos = new Point2D { x = x, y = y };

            if (moveCount == 1)
            {
                moveCount = 0;
                inDeep = false;
            }
            moveCount++;
        }
    }
}