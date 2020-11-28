## Unity ECS implementation of a typical Culling system supporting Frustrum Culling and Occlusion Culling
----------------------------------------------------------------------------------------
## Viewer perspective
![](https://raw.githubusercontent.com/vincent-breysse/culling/main/Screen/0.png)
![#1589F0](https://via.placeholder.com/15/1589F0/000000?text=+) **Not culled**  
![#f03c15](https://via.placeholder.com/15/f03c15/000000?text=+) **Culled by Occluders**  
![#888888](https://via.placeholder.com/15/888888/000000?text=+) **Out of Frustrum**  
## Debug view
![](https://raw.githubusercontent.com/vincent-breysse/culling/main/Screen/1.png)
![#1589F0](https://via.placeholder.com/15/1589F0/000000?text=+) **Not culled**  
![#f03c15](https://via.placeholder.com/15/f03c15/000000?text=+) **Culled by Occluders**  
![#888888](https://via.placeholder.com/15/888888/000000?text=+) **Out of Frustrum**  
## Debug view
![](https://raw.githubusercontent.com/vincent-breysse/culling/main/Screen/2.png)
![#1589F0](https://via.placeholder.com/15/1589F0/000000?text=+) **Not culled**  
![#f03c15](https://via.placeholder.com/15/f03c15/000000?text=+) **Culled by Occluders**  
![#888888](https://via.placeholder.com/15/888888/000000?text=+) **Out of Frustrum**  

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
## Objects not fitting in any Octree nodes are moved in a particular Root layer for special processing
![](https://raw.githubusercontent.com/vincent-breysse/culling/main/Screen/8.png)
![#aaFF11](https://via.placeholder.com/15/aaFF11/000000?text=+) **Object at Root layer**  

# Inputs
---------------------------------------------------------------
**W,A,S,D + Mouse** : Basic movements when using the Viewer camera  
**Mouse Right** : Rotate orbital camera around viewer when using the Debug camera  
**Mouse Scroll** : Zoom/Dezoom when using the Debug camera  


**Space** : Switch between Debug/Viewer camera  
**L** : Lock/Unlock camera movements  
**Alpha1** : Show/Hide stats panel  
**Alpha2** : Display next Octree layer  
**Alpha3** : Show/Hide objects at Root Octree layer  
**Alpha4** : Show/Hide object AABBs  
