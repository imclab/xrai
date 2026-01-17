# Mofo

This project is the engineering part of the [first lesson of the HoloKit tutorial](https://docs.holokit.io/for-creators/tutorials-and-case-studies/tutorial-1-use-hand-tracking-for-interacting-with-holograms). It covers topics such as how to use HoloKit stereo rendering, how to place AR objects, and interact with them.

## What is Mofo

Mofo is an augmented reality experience and one of the case studies in the HoloKit tutorial. It enables users to create a particle-style Buddha at any location, triggering interactive effects with hand gestures. Leveraging the HoloKit SDK, this experience supports stereo rendering, allowing users to enhance their experience with the accompanying HoloKit hardware.

After building this project as an iPhone app, first, scan surfaces(floor) around your, tap on screen to place a particle-style Buddha.

![Example Image](Documentation~/Images/placement.avif)

Utilizing the hand-tracking feature from the HoloKit SDK, this enables players to interact with the Buddha using their hands.

![Example Image](Documentation~/Images/hand-interaction.avif)

Additionally, the app offers stereo rendering options for an enhanced mixed reality experience using HoloKit hardware.

![Example Image](Documentation~/Images/stereo-rendering-mode.jpeg)

## About HoloKit Tutorial

### Introduction

The HoloKit tutorial aims to provide a series of engaging and hands-on courses, gradually exploring the AR development process and related technologies on the Unity platform. It also guides users on how to utilize the HoloKit SDK effectively.

### Link


In the following linked article, we build this application step by step from scratch. If you have the need to learn or reference, please visit: 

https://app.gitbook.com/o/mR4uhYDvPFKgusOgXgy0/s/GgGuLHKzjNAAsbVXzyoG/for-creators/tutorials-and-case-studies/tutorial-1-use-hand-tracking-for-interacting-with-holograms

## Requirements

This project utilizes the HoloKit SDK and aims to build an app that runs on iOS devices.

1. Unity 2022.3.8f1
2. Xcode 14.2
3. iPhone with Lidar capability

## How to try it

### Build App to your device

1. Clone the project, open with Unity
2. Open scene in path: Assets->Scene->Buddha_PlacingWithTouch
3. Build to an Xcode project, go to: File -> Build Settings -> Build, to build this scene to an Xcode project
4. Open Xcode, then open the project
5. Click build button to build app to your device

### Play

1. Open the app, scan on a flat surface(floor)
2. Tap on screen to create a Buddha
3. Get close and use hand to interact with Buddha
4. Click on “Stereo” button to switch rendering mode

## Change Log
