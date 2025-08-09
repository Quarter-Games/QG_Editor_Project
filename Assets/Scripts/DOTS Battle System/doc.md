## How it works

This is basically a tower defense where enemies spawn around the edges and move toward the center. Towers automatically shoot at the closest enemy, and when projectiles hit enemies, they take damage and die.

### The main parts:

**Towers** (`AutomatedTowerAuthoring.cs`)
- Automatically find and shoot at the closest enemy
- Have a cooldown between shots
- Fire projectiles in the enemy's direction

**Enemies** (`EnemyHealthAuthoring.cs`, `EnemyMovingAuthoring.cs`) 
- Spawn randomly around the battlefield
- Move straight toward the center at a set speed
- Have health that decreases when hit by projectiles
- Die when health reaches zero

**Spawning** (`EnemySpawnerAuthoring.cs`)
- Continuously spawns new enemies at timed intervals
- Places them randomly in a circle around the center
- Configurable spawn rate and distance

**Projectiles** (`ProjectileAuthoring.cs`)
- Shot by towers toward enemies
- Move in a straight line at constant speed  
- Automatically disappear after a set time

**Damage** (`ProjectileDamageAuthoring.cs`)
- Detects when projectiles hit enemies using Unity Physics
- Applies damage and destroys both the projectile and enemy (if health drops to 0)
- Uses a separate damage event system for clean processing
