# Reality by Example

[Reality by Example](https://ccrma.stanford.edu/~lja/vr/RealityByExample/) is a tool for creating an entire virtual world from within VR. 
Users can shape terrain, create interactive background music, and animate and sonify creatures by demonstrating what they would like.
Internal interactive machine learning models then give the user a best guess of what they wanted, extrapolating the demonstrated examples
across the entire world. From there, the user refines the examples they have given until they are satisfied.

The tool provides both low-level and high-level methods for shaping the world. For example, a user can provide an example that the terrain
should feature a certain texture in a certain position, or they can increase the variety of textures across the entire terrain. High-level 
methods allow users to find inspiration from randomness and slight shifts, and to approximate what they want; then, low-level methods can provide
the final polish.

One major goal for this tool is to make it difficult to make an overly unpleasant world. When it comes to music, rather than composing a score 
note by note, the user controls high-level parameters of the background music, such as chord changes, tempo, timbre, and textural density. This 
way, they can craft a narrative with sound, such as making the music sound more triumphant and energetic as you move up a mountain peak.

It was important to me to give the creatures that users create a sense of animus: I wanted them to feel like they were inspired at their 
core by the demonstrations that users give with body movements and microphone sounds, but at the same time that they also had a mind of their own. 
As such, the creatures have additional rules for their behavior depending on whether they are flying, swimming, or walking creatures. These rules are 
based loosely on the concept of [boids](https://en.wikipedia.org/wiki/Boids).

A concept that is suffused throughout the tool is that of relatedness and dependence. In a design pattern I call **chained mappings**, the outputs of 
some machine learning models are also used as the inputs for other models. This way, the appearance of a landscape or the animation of a bird can 
genuinely depend on other the properties of the world. These kinds of worlds feel interconnected, nuanced, detailed, and alive. For one example, the 
texture applied to the landscape is based not only on the coordinates of the examples, but also on the learned landscape height and steepness, enabling 
mountains with rippled boulders that are mossy on the side or snowy on the top. Creatures can also have their animations tied to landscape properties 
and musical properties, so you can create a flock of birds that knows to dive down off of a cliff, or all flaps their wings in unison to the music as 
it gets faster and slower.

## Running the Project

The tool can be run / compiled from the [CombinedUI](Assets/Scenes/CombinedUI.unity) Unity scene. Use the VR controller to summon a palette of the tools 
at your disposal. 

Since the entire world is learned from examples, you only need to save the examples themselves in order to load the world later. The 
[LoadExamplesInBrowser](Assets/Scenes/LoadExamplesInBrowser.unity) scene demonstrates this functionality using WebGL and WebChucK.
The world of [Jacklantis](https://ccrma.stanford.edu/~lja/vr/jacklantis/) was created over an interactive Zoom session and can be explored on the 
web in this way. (With ever-evolving browser standards, the project may break eventually!)

Another benefit of the world being entirely specified by examples is that it makes it easy to transmit over the network. See the `rbe-together` branch 
for the networked, multi-user version of Reality by Example.

## Scripts

Beyond what is detailed below, you might also want to delve into [movement](Assets/Scripts/Movement) through the world, 
general [VR UI](Assets/Scripts/VUI) functionality, and miscellaneous [utility scripts](Assets/Scripts/Utility).

### Terrain

Terrains are comprised of three underlying models.

- The base height model in the [ConnectedTerrainController](Assets/Scripts/Terrain/ConnectedTerrainController.cs) maps the (x,z) coordinates 
of examples to the desired (y) height of those examples.
- Then, the same component learns a refined "bumpiness" map, summing the relative strengths of GPS data of various types of landscapes onto the 
landscape. It can use not only the (x,z) coordinates of its examples but also the learned (y) height of the base height model as its inputs.
- Finally, the [ConnectedTerrainTextureController](Assets/Scripts/Terrain/ConnectedTerrainTextureController.cs) predicts the relative strengths 
of different visual textures according to the (x,y,z) and steepness of the terrain; the (y) and steepness are the output of the previous two models.

Each virtual world is represented by a grid of underlying terrain blocks, each with its own set of models. This way, different regions can provide 
specific examples without making the models of other areas brittle and hard to work with. The terrain blocks are smoothed into one another so that 
the entire world appears smoothly varying. After any changes are made in one terrain block, the surroundings ones are 
[stitched together](Assets/Scripts/Terrain/StitchAllTerrains.cs). 

### High-Level Methods

High-level methods affect all of the given examples on a specific tile. They fall under a few major categories:

- [Randomizer](Assets/Scripts/High-Level%20Methods/RandomizeTerrain.cs) methods create an unpredictable shift in the landscape to shake things up. 
This can be as small as a slight shift in each example to create a tile that looks slightly different from the current one, or as drastic as 
completely erasing the world and creating a new one in its place.
- [Continuous](Assets/Scripts/High-Level%20Methods/HighLevelTerrainMethods.cs) high-level methods give the user a real-valued number to adjust. 
Changes in this number affect all the underlying examples. For example, increasing the bumpiness variation causes the examples to take on a 
greater number of categories of GPS data and to use said data more strongly in the final mapping.
- [Discrete](Assets/Scripts/High-Level%20Methods/HighLevelTerrainClickMethods.cs) high-level methods give the user the control to shuffle around 
which examples represent which categories of data. For example, a green mountain with snow on its peak might be swapped so that it instead has white 
sand around its base with a green top. The shuffles are random, but users can undo/redo back and forth between all shuffles that have been performed 
so far.


### Sound

The [sound engine](Assets/Scripts/Sound/SoundEngine.cs) translates the high-level musical parameters learned in various machine learning models from 
the [input features](Assets/Scripts/Sound/SoundEngineFeatures.cs) into a ChucK program that specifies tempo and chord changes. As such, this module 
responds directly to changes in the current output value of the [chord classifier](Assets/Scripts/Sound/SoundEngineChordClassifier.cs) and the 
[tempo regression](/Assets/Scripts/Sound/SoundEngineTempoRegressor.cs).

Each instrument in the background music is then responsible for interpreting each of the remaining high-level parameters, which are abstractly valued 
from 0 to 1. These parameters are [timbre, textural density, and volume](Assets/Scripts/Sound/SoundEngine0To1Regressor.cs). The instruments that 
interpret these values and refer to the correct tempo and chord changes are the granular synthesis 
["ahh" chords](Assets/Scripts/Sound/SoundEngineAhhChords.cs), the modal bar [arpeggio](Assets/Scripts/Sound/SoundEngineModalArpeggio.cs), and the 
[shakers](Assets/Scripts/Sound/SoundEngineShakers.cs).

### Animation

The [creature controller](Assets/Scripts/Animation/AnimationByRecordedExampleController.cs) coordinates between several systems 
necessary for animating and sonifying a creature formed from examples performed by the user.

Each creature is animated by calculating its position, rotation, and the relative positions of its limbs such as wingtips, feet, or flippers. 
These are predicted using information from the underlying terrain as input data.
Rather than predicting these values independently and frame-by-frame, which results in somewhat random and alien seeming movement, the models for 
animation instead calculate the relative strengths of weighted sums of each individual animation recording. This way, the creature appears to move 
between different specific behaviors shown in the examples, but recombines them in unexpected ways.

Creatures also make sounds based on the sounds users make into their headset microphones during their recordings. Since the timbre of a sound is 
recognizeable at a shorter time scale than an animation can be recognized at, this mapping is implemented using a granular synthesis of the sounds 
recorded during the example. Every 25 milliseconds, the [sound model](Assets/Scripts/Animation/AnimationSoundRecorderPlaybackController.cs) predicts 
which section of the recorded audio best aligns with where the creature is and what it is doing.

Finally, in addition to standard [boids rules](Assets/Scripts/Animation/AnimationByRecordedExampleController.cs#L548) that prevent animals from getting 
too close to each other or too far away from where their examples were recorded, each type of creature also has more specific rules. Flying creatures 
avoid hitting water and land. Swimming creatures avoid swimming above the water or below ground, turn around when faced with cliffs, and 
[rotate their heads](Assets/Scripts/Animation/ControlFish.cs) toward where they are swimming. Land creatures turn to the side when they come across water 
and can [animate some legs based on others](Assets/Scripts/Animation/ControlLandAnimal.cs) if the creature has more limbs to animate than humans have arms. 

