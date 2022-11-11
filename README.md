# Reality by Example: Together

RBE Together is a networked, multi-user version of Reality by Example (see `main` branch). The goal of this evolution of the RBE prototype was to explore the 
effects of social relationships and communication restrictions on the creative process of creators working on building worlds in VR. As such, not only does 
RBET sync the virtual world between all its inhabitants, it also gives them weird and whimsical methods of communicating with one another.


## Running the Project

The [networked blank slate](Assets/Scenes/BlankSlate_Networked.unity) scene allows users to occupy a shared virtual world. The first person to open 
the client will be the "builder", the owner of that world. The rest of the people to open the client will be "helpers", capable of communicating with 
the builder and seeing their changes in real time, but not of making changes to the same scale as the builder. This restriction was intentional (by default, 
anyone could use any creation tool), and was based in an understanding from ethnographic research that users might want to maintain a sense of personal 
ownership over their projects.

## Scripts

The networked syncing of this project is based on the [Photon Engine](https://www.photonengine.com/). Since each world is learned entirely from examples, 
we need only notify other clients of changes made to individual examples. Then, each client re-learns its underlying models based on the new training data.

The properties that are synced between clients include:

- Terrain [bumpiness](Assets/Scripts/Photon/PhotonGISExampleView.cs) and [texture](Assets/Scripts/Photon/PhotonTextureExampleView.cs) (terrain base 
height examples rely only on 3D position and have no special properties to sync)
- [Musical parameters](Assets/Scripts/Photon/Photon0To1ExampleView.cs), including [tempo](Assets/Scripts/Photon/PhotonTempoExampleView.cs) 
and [chords](Assets/Scripts/Photon/PhotonChordExampleView.cs)
- The [animated position](Assets/Scripts/Photon/PhotonAnimatedCreatureView.cs), [sound](Assets/Scripts/Photon/PhotonAnimatedCreatureSoundView.cs), 
[name](Assets/Scripts/Photon/PhotonAnimatedCreatureNameView.cs), and [color](Assets/Scripts/Photon/PhotonAnimatedCreatureColorView.cs) of creatures
- Properties related to each user's avatar, such as its [color](Assets/Scripts/Photon/AvatarColorUpdater.cs) and 
[communication synthesizer](Assets/Scripts/Photon/PhotonCommunicateSynthView.cs) (detailed below)

The animations of avatars and creatures are also [interpolated](Assets/Scripts/Photon/PhotonLerpTransformView.cs) at the local level, to avoid any 
jitteriness resulting from inconsistencies in the network.

## Communication Between Users

In order to foster a whimsical, creative mindset, users communicate with each other using playful, simple tools. The most basic of these is a geometric 
avatar, which users can use to show where they are looking and make hand gestures. Their head is represented by a prism, and their hands by spheres.

Each user has a [communication synthesizer](Assets/Scripts/Sound/CommunicateSynth.cs) that they can control with hand movements. They have different 
[mappings](Assets/Scripts/Sound/CommunicateSynthMapping.cs) available to them, and can also generate their own mapping using interactive machine learning.
This process involves making hand movements while vocalizing into the headset microphone; then the communicator regression learns how to map hand movements 
to the pitch, volume, and timbre of the synthesizer. To further customize their personal mapping, users can turn on and off individual features. Users tended 
to use the synthesizers to communicate changes over time and space, and to add emotional valence to their other communications.

Finally, each user can create drawings in the air or on top of the landscape itself. These are implemented with Unity's 
[particle systems](https://docs.unity3d.com/Manual/class-ParticleSystem.html) and a laser pointer for the landscape drawings. The drawings can be set to 
fade quickly or persist as long as the user doesn't run out of virtual "ink". These drawings were used to communicate more concrete ideas, and were also 
often used by the helpers to decorate the world at a smaller, human scale.
