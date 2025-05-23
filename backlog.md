# Backlog

## Meta
* Recording
    * https://www.screentogif.com/
    * OBS Studio

## Epics

* Generate monsters, items, etc. using https://www.dnd5eapi.co/
* An actual UI with player info
* Create and render roguelike indoor map
* Generate a more interesting dungeon.
    * Inspirations 
        * Brogue: https://www.rockpapershotgun.com/2015/07/28/how-do-roguelikes-generate-levels/ and Brogue youtube talk
        * http://roguebasin.roguelikedevelopment.org/index.php?title=Abstract_Dungeons
        * https://www.reddit.com/r/roguelikedev/comments/6df0aw/my_implementation_of_a_bunch_of_dungeon_algorithms/
        * http://roguebasin.roguelikedevelopment.org/index.php?title=Grid_Based_Dungeon_Generator
        * Tematiser dungeon. Fx smidt i fængsel i borg, starter uden noget i fangekælder. Derefter opad. Generer semitilfældige levels med tema. 
* Update to most recent version of Blazor
* Add sound and music
    * Freesound - pack: Fantasy Sound Effects Library by LittleRobotSoundFactory
    * bfxr
    * NOTE Currently requires Autoplay to be allowed in browser


## Current tasks
* Keyboard interaction
  * Allow holding a key down - possible use of the keypress event rather than the keyup event
  * You should be able to open a door, just by walking into it (spending a single turn)
* Go through TODOs in code, and prioritize/fix/delete
* Story
* On-going: Prettify selection of floor tiles
    * Ideas:
        * Animated torches may "light" area around them (can I do something to darken or ligthen the image?)
* Place a few more floor-tile decorations (from bottom of uf_terrain)
* Focus
    * Signal to player whether the tilemap has focus or not. For instance, by greying out the map if it is not in focus.
* Add a few monsters
    * DONE Goblin
    * DONE Random walk
    * DONE Restrict walk to when awake -> initial state: asleep, wake when player sees it, 
    * Fall asleep again k turns after last seeing player
    * DONE Spider 
    * DONE AI: Move direction towards player
    * AI: Move shortest path to player
    * DONE Component: Fighter - hps and stats; Fighting actions delegate to Figthing system
    * DONE Add to Player and Monster
    * DONE AI: Basic close combat fighter
    * AI+Component: Combat Spider shoots spider web - chance of lost move
        * dungeon gen upping chance of spider webs near
    * Monster idea: Roguelike - monstre der graver sig ind fra gulvet eller væggen
    * Add Player death
* Fighting system
  * Fighting and stats a la wfrp 4th
    * DONE Basic stats 
    * Crits, shields, etc.
  * Combat visual fx - req. temporary effect system
  * DONE Leave bloody mark after killed monsters
* Roguelike - implement viewport for larger maps; probably also necessary for displaying lives, UI and so on.
* Interesting traps:
  * Trigger that starts spiked wall towards player.
* Player should be able to use interactive objects
    * DONE To open doors

### BUGS

## Coming tasks
* Shooting 
* AI: Ranged fighter
* Create simple custom room-parser
    * Or read a format from Tiled?
* Read some config stuff for DungeonGen from a config file - or at least another cs file with consts and so on.
* WIP Use a single Random in a public static class, rather than all over


### Possibles, and considerations
* Put all moving stuff in a Mobile Component?
* Generic optimization
    * http://roguebasin.roguelikedevelopment.org/index.php?title=Data_structures_for_the_map
* Floodfill: https://github.com/azsdaja/FloodSpill-CSharp
* Possible use Dijkstra maps http://www.roguebasin.com/index.php?title=Dijkstra_Maps_Visualized
* Possible Tarjan's minimal cut or (Tarjan's strongly connected components algorithm)
* Possible future problem: Currently moveables can't block light.
* Could enforce dec in GameObject tile, by not letting it have coords itself?
* Note: Right now, I point to GameObjects via two collections; one 2-dimensional array (width, height) allowing me to find GameObjects by coord. This has proved useful several places; both while rendering, updating graphics efficiently, and in checking for presence of certain GameObjects in a coord. It seems quite reasonable that I need this. As in my pyroguelike, I also have GameObjetcts in a flat list. Right now, I actually don't use that for anything. Makes me wonder if that is superflouos, or whether I'll meet a design-issue at some point.
* https://visualstudiomagazine.com/articles/2018/08/01/integrating-blazor-javascript.aspx?m=1
* Possible from roguelike to turn based strategy?

### Notes

* Roguelike - connectivity of graph ... Tarjans minimal / Karger's cut / edge connectivity
TODO: Had to set network.http.spdy.enabled.http2 to true in Firefox

### Ideas

* A game, where you are your merc army's scout / forager
* Space hulk spil a la det gamle som roguelike?
    * Eller tilsvarende brætspil inspirerede? 
* Prøv at implementere 2nd ed combat også 
* Start as a young on a battlefield / an invaded town
    * Maybe you're a highlander-type being. Your goal - revenge. 
* 

## Done tasks

(Not at all complete...)

* Added basic background sound loop
* Animations can now also be offset
* Add animated torches
* Fix Spiderwebs so they're also rendered based on the tile they stem from (w offset)
* BUG: (visual): Setting opacity for gameobjects - such as doors - on top of other stuff, looks weird as they become actually transparent
* BUG: Exception from this.StateHasChanged (on keypress), after a refresh of page...
* Due to the problem with moveables and XXXByCoord, MoveAbles can't be blocking currently
    * DUPE of BUG: Something fishy in my impl of Moveables and MoveableByCoord. Doesn't seem linked. Player itself adds it's dec.
* Implement FoV
    1. DONE To get visuals right - very simple algorithm, with a sight radius of *k*, with no tile blocking vision
        * Every tile not immediately visible is dimmed.
        * The same goes for all GameObject's
    2. DONE Implement mapping: Once a tile has been visible once, it is mapped (as per http://roguebasin.roguelikedevelopment.org/index.php?title=Data_structures_for_the_map).
        * If a tile hasn't been mapped it is not rendered, neither are it's contents.
        * If a tile has been mapped, it is rendered, but dimmed (as per step 1.)
    3. DONE Implement a more correct algorithm - one of http://www.adammil.net/blog/v125_roguelike_vision_algorithms.html, probably
    4. DONE What about non-static GameObject's?
            * Wall-dec' GameObject's and the like should be rendered, iff the tile is rendered.
            * ... Add a property to GameObject's, to mark that they are not permanent decorations. If they are not, then they should be rendered iff they are visible. 

* Added basic special rooms - large rooms with prettier floor-tiles
* Fix buggy placement of doors
* Player respects floors, walls and doors.
* Player with basic keyboard movement
* Render animated player 
* Create horizontal tunnel
* Render open and closed doors - horizontal
* Add half-walls (as decorations?) to all bottommost (defined how?) walls
* Optimize clickable decorations - currently every click invokes a total rerender of all gameobjects => decorations
* Alternative rendering of GameObjects - by requiring all grafix to be rendered on the tile; but may be able to use css styling of graphic to offset up/down.
* Make door clickable -> toggle open/close door
    * Catch mouse click at coord
    * Invoke and try out css animation-system
    * (This will probably be a bit, since I know absolutely nothing about css animation, and only minor stuff about catching mouse clicks. I don't really know a lot about web...)
* Render closed and open vertical doors
* Render tunnel
* Create horizontal tunnel function
* Render room
* Create room function
* Render gameobjects on top - graphic effect may extend beyond tile of gameobject
    * Current approach -> gameobjects are a flat lists of things, that are rendered through a prepass to one or more decorations located in tiles
    * => every time gameobjects change, a RenderGameObjects() should be called.
* Render a basically meaningful map
