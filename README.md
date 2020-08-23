# Commander

Small RTS game made to explore graphic design, 3D modelling and working with Unity game engine.

## Context

Developed as project deliverable for course `MTI835 - Développement d'applications graphiques` of 
`École de technologie supérieure (ÉTS) de Montréal`.

The game is greatly inspired by `Command and Conquer` series.

## Documentation
 
The project is not a complete game. It only provides basic operations for the moment, including : 

- Models for differnt type of tanks each with specific set of feature (arc of fire, mobility speed, etc.).
- Bulldozer unit for building creation on the placable map area.
- War factory building that can produce the set of different tank units with queue.
- GUI with minimap, edge-scolling, zooming and arrow-keys for movement across the map area.
- Minimalisting GUI controls for building placement, orientation and collisions.
- Basic obstacles such as trees and building that provide classes to handle traversable grid map positions.
- Attack and movement commands using an improved A* pathfinding algorithm.
- Projectiles and sprites to simulate unit smoke movement, explosions from attacks and destruction.
- Animations for moving turrets and destroyed units removal.
- Units selection and health bar display with taken dammage.

Demonstration [videos](./videos) are provided to illustrate funtionalities described above.

Documentation report as well as presentation summary (both in French) are available in [docs](./docs). 
These are the deliverable of the course, but are provided as reference for further information about 
choices made during implementation, details about intented behaviour and design components.
