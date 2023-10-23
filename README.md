# ConspiracySurvivor
A open world survival multiplayer game prototype made with unity.

![Ingame](https://mikespykermann.de/Images/ConspiracySurvivor/Screenshot%202023-10-04%20144602.png)

> [!NOTE]
> Some assets had to be removed for copyright reasons: Models, Textures, Sounds. But code and settings are intact.

Feel free to learn and take from this project what you need.

See https://mikespykermann.de/conspiracySurvivor for details and build download.

## Introduction

Inspired by the open-world survival game trend at that time, I wanted to find out if I could create my own game in this genre. I had some unique ideas too.

Sadly, this project was way out of scope for one developer, so I decided to abandon it.

## The Idea

This game was very much inspired by rust. So I took many concepts and ideas from there.

But I also wanted to do something very different. I was always fascinated by conspiracy theories. More precisely: The exciting once. Like Reptoids, Nazi UFOs and secret government projects. And I wanted to incorporate many of these conspiracy theories into my game as random events. For example, at random intervals, the chem-trail-event will be triggered: A plane is visible in the sky. All players must now put on a gas mask or go indoors to survive. Or another example: Reptoids landed on earth and are now on the hunt for the players. The players must fight back or hide. Unfortunately, these ideas never made it into the game.

## The Features

Although this game is far from finished, there are a lot of features I polished to a decent level. Mostly on the code side.

- Random world generation: The hole map is randomly generated except for some POIs that are merged into the world. There are different biomes, like beach, forest, grassland, and mountain. Everything but mountains is based on perlin noise. Mountains are created using the diamond-square algorithm. The world generation started out as C# code that got executed on the CPU but was moved to a compute shader to run on the GPU for faster results.

- Terrain system: What you can see in this game is not the Unity terrain system. I created my own terrain system to better handle the ground textures and to use my own shader. I also wanted to be able to cut out holes in the terrain. That was not possible with the Unity terrain system at that time. Another reason for my decision was the overhead the Unity Terrain System had and the lack of control it offered me. The idea for my system is simple. I have a height map and a texture map, and the system creates and manages textured meshes at the right LOD level according to the player's position based on these maps. On the server, the system handles multiple player positions and creates meshes that are used for colliders only.

- Building system: This system allows the player to place building foundations anywhere in the world. There is a triangle, a square, and, for the sake of being unique, a pentagon foundation. On these foundations, the player can expand the building with walls. The walls can be modified. It is possible to freely place a door frame or a window frame on a wall. This way, the player can, for example, choose to have a window near the ground or near the ceiling. I also experimented with stability so that there is a limit on how tall a building can be.

- Network system: I implemented my own network system based on TCP and UDP. It is all written in C#. I also created a system to easily identify network game objects and exchange messages between the server and client. All that is necessary to use this functionality is to derive a C# class from the base (network) entity class. This class will then have access to multiple methods for sending and receiving network messages. These methods differ in the network protocol used and whether they are on the server or the client side. On the server side, there are options to send to one or all clients.

- Inventory system: Every player has their own inventory that can be filled with items that drop in the world. This system allows the player to move items from and to containers. There is a hotbar for usable items. It is all synced over the network, too.

- Sound system: Inspired by the "Soundscapes" system I encountered while creating maps for Half-Life 2 I wanted to create something similar. The way it works is that it identifies in which area or biome the player is. It then has a series of sounds to choose from for this area. Some of the sounds are looping, and others occur randomly and play once in a random spot in the 3D world. This system is best observed in the beach biome.
