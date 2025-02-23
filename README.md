# Boiding Fish Algorithm
Here's a fish schooling algorithm I made in Unity for a procedural content generation course.
This works off of Craig Reynold's 4 main forces of boids: flock centering, velocity matching, avoidance, and wandering.
Each force can be toggled individually for unique fish behavior!

The fish are constructed out of only Unity primitives as per project requirements.
They are also animated entirely via script.

## How to run
Install Unity and run the project in editor.

You can toggle the following items in the inspector:

- Flock Centering / Cohesion
    - Fish steer towards center of mass of local school
    - Disable to remove the main grouping force of the boid
- Velocity Matching / Alignment
    - Fish steer towards the heading of local school
    - Disable to see discordant schools of fish
- Avoidance
    - Fish repel each other more and more when they get near
    - Disable to see clusters of fish overlapping each other
- Wandering
    - Fish traverse on their own with a random noise factor
    - Disable to see fish stream in 
- Bubble Trails
    - Shows path of each fish to better observe movement
    - Disable for clarity
- Wall Avoidance
    - Fish avoid / rebound off of tank walls
    - Disable to see fish school off into the endless void

 You can also dynamically adjust tank size by adjusting the bounds on the tank game object.
