# TODO: 
- Improve UI to display info about the state of the node and to accomodate headset settings.
- Refactor the code according to the desired SDK.

# Summary

The project aims to provide a customizable ros-compatible EEG acquisition node wirtten in C# for the ROSNeuro architecture.
The project leverages sharp-ros library to implemente the bridge. 
Internally rossharp use websockets to integrate with the rest of the architecture. The standard way to provide this integration is via rosbridge_server, on roscore side.


## Setup rosbridge_server

1. install the dependency for ROS:    

```sudo apt-get install ros-<ROS_DISTRO>-rosbridge-suite```


2. create a launcher     

```vi start_bridge.launch```

example of start_bridge.launch    

```
<launch>
  <include file="$(find rosbridge_server)/launch/rosbridge_websocket.launch" > 
     <arg name="port" value="8080"/>
     <arg name="address" default="192.168.1.37" />
  </include>
</launch>
```

3. start the bridge    

```roslaunch start_bridge.launch```



# External references:    
ROSNeuro: https://github.com/rosneuro     
rosbridge_suite: http://wiki.ros.org/rosbridge_suite    
ros-sharp: https://github.com/siemens/ros-sharp    
