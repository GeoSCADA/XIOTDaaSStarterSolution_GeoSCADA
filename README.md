# XIOTDaaSStarterSolution_GeoSCADA

This sample project shows how to get the incoming data stream from the cloud XIOT solution and feed it into points in Geo SCADA.

To build this for your version of Geo SCADA, add a reference to c:\Program Files\Schneider Electric\ClearSCADA\ClearSCADA.Client.dll (remove the current one and replace with yours to build for that version of Geo SCADA).

Ensure you set the Geo SCADA parameters for node, user and password.

Configure the ServiceBusConnectionString for the XIOT cloud service.

XIOT devices are identified uniquely by a "device" property, but this is a non-friendly name.

The "station" property of the XIOT device can be used as a friendly name.

We will use the "device" property and a fixed prefix "TagBase" to construct a group name, but if not found will try to use the "device" name instead.
e.g. "My XIOT Devices.326D" then "My XIOT Devices.88AC1A"
Then add the tag property as the point name (analog, digital or string Internal point)
e.g. "My XIOT Devices.88AC1A.S1State"

This project requires you to create points in ViewX before retrieving the data. It could be modified to create points automatically. You may wish to ensure that Analog and Digital points have Historic data enabled.

