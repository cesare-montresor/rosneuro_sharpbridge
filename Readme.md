** This project is a stub **

# Summary

The project aims to provide a customizable ros-compatible EEG acquisition node wirtten in C# for the ROSNeuro architecture.
The project leverages sharp-ros library to implemente the bridge. 
Internally rossharp use websockets to integrate with the rest of the architecture. The standard way to provide this integration is via rosbridge_server, on roscore side.

# Setup rosbridge_server

## install the dependency for ROS: 

    sudo apt-get install ros-<ROS_DISTRO>-rosbridge-suite

## create a launcher 
    vi start_bridge.launch

## start_bridge.launch
    <launch>
      <include file="$(find rosbridge_server)/launch/rosbridge_websocket.launch" > 
         <arg name="port" value="8080"/>
         <arg name="address" default="192.168.1.37" />
      </include>
    </launch>

## start the bridge
    roslaunch start_bridge.launch


# Known issues:
- inncorect value-passing between thread
- Data type it's a mtd_msg.String
