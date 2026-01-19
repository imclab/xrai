# Tentacus 

A Mixed Reality Ritual for Distributed Bodies and Emergent Minds

In Tentacus, dancers form a living organism — a decentralized body with many minds. Holding left hands together in a continuous ring, they ground themselves in shared presence. Their right hands, free and curious, reach outward to explore a digitally layered world, sensing, touching, and activating hidden phenomena.

Each dancer is a tentacle of the Tentacus: a being with no central brain, but intelligence distributed across limbs. Inspired by the neurophysiology of the octopus, where each arm thinks independently yet moves in concert, this performance explores a new choreography of cognition — one enacted through protocols of movement, somatic alignment, and mixed reality augmentation.

The ritual becomes a protocol. The protocol becomes a creature.

Tentacus is not danced by dancers. It is danced through them.

## Overview

Tentacus is a collective embodiment experiment in mixed reality. Each performer wears a lightweight glove interface, enabling haptic with a shared digital layer. Through a left-hand network (symbolizing continuity) and right-hand exploration (symbolizing distributed agency), Tentacus models a decentralized intelligence system — inspired by the octopus and its limbic cognition — to challenge human-centric notions of coordination, control, and selfhood.

## Project Environment

Please be aware that these boilerplates are designed exclusively for iOS devices.

We have successfully tested them with the following software versions:

- Unity 6
- Xcode 16
- iOS 18

In theory, other versions of Unity and Xcode that are close to these should also be compatible. However, if you encounter any issues during the build process, feel free to raise an issue in the repository.

## How to Use 

Please print out the QR code and place it in a visible location to colocated all players. Make it 15cm x 15cm sharp in the physical dimension. 

https://github.com/holokit/holokit-image-tracking-relocalization/blob/main/Assets/Images/T_QRCode.png

## Change the host IP

Change 192.168.1.100 to real host IP


## External Marker Relocalizaion Boilerplate

![ezgif-5-d02131cab1](https://github.com/holokit/holokit-colocated-multiplayer-boilerplate/assets/44870300/0c986977-c469-4f3d-8158-65ff1ef53902)

The process is illustrated in the accompanying GIF: an iPhone tracks an external marker image. We enhance accuracy in image tracking by tracking multiple consecutive poses of a marker, minimizing the deviation that can occur with a single detection. Upon detecting a stable sequence, it computes the correct pose for this sequence and resets its coordinate origin accordingly.

### Local Router Connection

This method contrasts with MultipeerConnectivity in that it requires the presence of a router and manual input of the IP address. However, like MultipeerConnectivity, it also achieves low latency by facilitating local message transfer.

A prime scenario for employing a local router connection is in exhibition AR projects, where stable connections with multiple devices are crucial. A typical setup might involve a Mac or PC serving as the host, with client iPhones connected to it. Utilizing a Mac or PC as the host provides a robust computational core to manage operations. Additionally, this arrangement allows developers to monitor all connected clients and intervene in real-time, ensuring an optimized AR experience.

## Troubleshooting

### TextMesh Pro Essential Resources are missing. 

To resolve this issue, you need to import the TextMesh Pro Essential Resources. This can be done through the Unity Editor by navigating to the menu bar at the top of the screen. From there, you should select `Window`, then `TextMeshPro`, and finally `Import TMP Essential Resources`. This action will automatically import the required assets into your project, ensuring that TextMesh Pro operates smoothly and as expected.
