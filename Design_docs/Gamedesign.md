Gamedoc:
What this game about ?
3D game on adroid\win.
Game build from set of maps. Every map has 2 main point of interest, player base and enemy base.
On player base we can build barracks where spawning troops.
By after giving order troops running in towards the enemy and attack.
From killed enemy drops resourse type 1, for killed allies dropped resource type 2.
Resources used to build more barracks and upgrade army.
Goal - kill all enemy units using troops from barracks.


1. Map.
Game consist set of map with highten difficulty.
Every map its medium size 3D terrain that contains player and enemy base.
Map 1 - 2 barrack slots
Map 2 - 3 barrack slots
Map 3 - 4 barrack slots

2. Troops.
Allied troops spawn from barracks and gathering at training field.
Enemy troops spawn at certain stationar point at the game start

Game have 3 type of troops.
Every troops have same set of characteristic types with different values:
1. MoveSpeed
2. ProjectileType
3. Health
4. AgressiveRange
5. AttackRange
6. AttackSpeed
7. SpawnSpeed

Projectiles ignore allied troops.
1 projectile can hit only 1 enemy, after it destroed.

Player troops spawnin one by one from from available barracks.
They move only when player give an order, before that they accumulate on "training field" until field is full.
When they recieve charge order they all start moving to nearest enemy and attack.

3.Bases.
Enemy base contains 1 structure - flag.
Flag its circle zone. Every enemy troop "paints over" constant part of terrain, this "claimed" terrain visually connected to flag zone.
(not affect gameplay, only visual)

Player base have:
1. Update zone. Zone where player can spend resources to update Army level.
2. Training field. Rectangular zone with fixed count of slots where troops accomulate.
3. Barracks zones. In this spots player can spend resources to build barrack.
Every barrack have their own zone
Every barrack have their own troop type, its a type of troops that he spawn for fill training zone.
Every barrack have their own troop spawn speed.

4. Resources.
From every 1 dead allied to player troops drops 1 silver.
From every 1 dead enemy to player troops drops 1 gold.
Gold can be spent to upgrade Army level
Silver can be spend to build barracks.

5. Upgrades.
Army upgrade. Have 3 tiers.
Every tier allow to build new barrack type(new unit).
There 3 barrack types: soldier, veteran, master
For any Army update unlocked 1 type of barrack
Army level 1(default) - soldier barrack, 1 slot for barracks
Army level 2 - veteran barrack, 2 slots for barracks
Army level 3 - master barrack, 3 slots for barracks

Level end:
When player upgrade Army on 1 level on each tier.
To complete level 1 upgrade from tier 1 to tier 2
To complete level 2 upgrade for tier 2 to 3
To complete level 3 upgrade for tier 3 to end

End tier its plug, there no level 4, but in config exist cost.

6. UI
1. Main menu with button start.
2. Indicator of resource 1 and resource 2 on top of the screen.
3. Indicator(progress bar) with remaining enemy troops.
4. Next level window with 1 button - next level.
5. Choose barrack type to build (1-3 types based on current level)

Example of game flow.
1. Player open app
2. Player click on start game button
3. Loading level 1
4. Spawned map with enemy flag, enemy troops near flag. Territory that occupied enemy connected to flag zone.
5. Spawned player, 1 slot for barrack, training zone, Army upgrade zone. On start player have resource 1 to build 1 barrack
6. Player move with joystick over player model and stay on barrack slot
7. Popup shows with 3 possibility to build (1 unlokced, 2 locked)
8. Player choose variant 1(soldier barrack)
9. On barracks slots appear barrack and start produce troops.
10. Every new troop appear and move in empty slot at training field
11. Player can move in to Order zone(near training zone) and when he move in this zone - order has given
12. When order given all barracks stop produce and wait untill all ordered troops are dead to start produce again.
13. Troop cant be produuced if troops are over limit even if he already in produce process
14. Troops moving from training zone to nearest enemy.
15. Enemy and player troops fire each other(nearest target)
16. After death enemy troops leave gold, and player troops leave silver
17. If player trups kill enemy they move to another enemy, if there no another enemy they move back to training field.
18. If enemy kill all player troops they move to default position, and barracks start produce again
19. If Player Upgrade Army to next tier - level complete. Showing popup.
20. Using silver player can build new barracks on available barracks zones.

Player.
Player have only 2 stats:
1. Health
2. Move speed
He can't attack and can only gather resources or build.
Enemy troops can attack player

Camera movemnts:
Camera always follow player, except end level animation.

Player movements:
Player moving with stick showing over player for android, and with WASD on PC.

Map building:
Every map its prefab that spawning on single scene.
Every map builded with hands without automatization.
Enemy troops, player, enemyFlag, armyUpgradeZone, barrack slots, training field are seted with game designer.

Systems:
Root - MonoBehaviours that wire all scene dependencies
Control - MonoBehaviour that can read data and set data to model, he have no public field and methods(Except method Tick).
 Control can be binded with presentation model. Control fully data driven.
 
PresentationModel - C# class that handle all logic for view to visualize. Presentation model can use other presentation models and models.
 Control can subscribe on presentation models fields and set data to methods.
 
Service - C# class. global controller that operate only with models, have no public fields or methods.

DataModel - C# class. Pure data, no logic, no methods.

Controls\DataModels cant have any injections.
Service\PresentationModels can have injection with one method - SetDependency. All dependencies must be wired using this ONE SINGLE method.

Scope:
1.Control operate only with his binded model, all his state is driven by state model. He doest know about else models or service. He can have reference to another 
Controls that he can bind with coresponding state models from his binded model.
2. Presentation models can only operate with another presentation models. Presentation model doenst know about controls, roots or service. It can only change
inner state and change self reactive fields.
3. Service can operate only with presentation models, service can recive presentation models.
4. Data models cant operate with anything, its pure data.


Boot Scene:
- VContainer installer for bind configs
3 type of config(scriptable).
1. Level config.
2. Troops config.
3. Projectile config

Menu Scene:
- MenuRoot. Receive MenuPresentationModel that have current level. MenuRoot have reference to StartButtonControl and on click loading level.


GameScene:
- GameRoot. Have references on controls:
1. EnemyFlagControl
2. List of enemies spawn points(to spawn with virtual barrack).
3. Player
4. Barracks slots list
5. Training field
6. ArmyUpgrade zone