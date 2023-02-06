# XIOTDaaSStarterSolution_GeoSCADA

**********************************************************************
SAMPLE CODE FOR EDUCATION ONLY. THIS CODE MAY NOT BE CYBER-SECURE.
YOU ARE RESPONSIBLE FOR IMPLEMENTING CYBER-SECURITY FEATURES.
**********************************************************************

Sample code of data export tools using the ClearSCADA / Geo SCADA APIs.

This is offered as source code which you can build with Visual 
Studio. It is not supported or warranted in any way by Schneider Electric.

The source code is available for you to freely use, modify and extend to 
suit your requirements or that of your clients. It is perhaps not the 
most optimized, efficient or elegant code but we hope that its simplicity 
will encourage engagement with Geo SCADA.

This sample project shows how to get the incoming data stream from the cloud XIOT solution and feed it into points in Geo SCADA.

To build this for your version of Geo SCADA, add a reference to c:\Program Files\Schneider Electric\ClearSCADA\ClearSCADA.Client.dll (remove the current one and replace with yours to build for that version of Geo SCADA).

Ensure you set the Geo SCADA parameters for node, user and password.

Configure the ServiceBusConnectionString for the XIOT cloud service.

XIOT devices are identified uniquely by a "device" property.

We use the "device" property and a fixed prefix variable "TagBase" to construct a group name.
e.g. "My XIOT Devices.88AC1A"
Then add the tag property as the point name (analog, digital or string Internal point)
e.g. "My XIOT Devices.88AC1A.S1State"

This project requires you to create Internal points in ViewX before retrieving the data. It could be modified to create points automatically.
You may wish to ensure that Analog and Digital points have Historic data enabled.

