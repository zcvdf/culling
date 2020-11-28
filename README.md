## Unity ECS implementation of a typical Culling system including Frustrum Culling and Occlusion Culling
----------------------------------------------------------------------------------------
![#1589F0](https://via.placeholder.com/15/1589F0/000000?text=+) **Not culled**  
![#f03c15](https://via.placeholder.com/15/f03c15/000000?text=+) **Culled by Occluders**  
![#888888](https://via.placeholder.com/15/888888/000000?text=+) **Out of Frustrum**  

## Viewer perspective
![](https://raw.githubusercontent.com/vincent-breysse/culling/main/Screen/0.png)
## Debug view
![](https://raw.githubusercontent.com/vincent-breysse/culling/main/Screen/1.png)
## Debug view
![](https://raw.githubusercontent.com/vincent-breysse/culling/main/Screen/2.png)

## Octree layer 0
![](https://raw.githubusercontent.com/vincent-breysse/culling/main/Screen/3.png)
## Octree layer 1
![](https://raw.githubusercontent.com/vincent-breysse/culling/main/Screen/4.png)
## Octree layer 2
![](https://raw.githubusercontent.com/vincent-breysse/culling/main/Screen/5.png)
## Octree layer 3
![](https://raw.githubusercontent.com/vincent-breysse/culling/main/Screen/6.png)
## Object AABBs
![](https://raw.githubusercontent.com/vincent-breysse/culling/main/Screen/7.png)
## Objects not fitting in any Octree node are moved in a particular Root layer for special processing
![#aaFF11](https://via.placeholder.com/15/aaFF11/000000?text=+) **Object at Root layer**  
![](https://raw.githubusercontent.com/vincent-breysse/culling/main/Screen/8.png)
