# holovision
Holovision is an example for use of Microsoft Computer Visopn API with Hololens
![picture](https://github.com/martdob/holovision/blob/master/hl1.png)

Instructions:
1.	You will need a Microsoft Cognitive Services account and a subscription key for the Computer Vision API.
If you already have an Azure account you can skip this step. Setup a Computer Vision App directly in your Azure subscription. To do that go directly to the Azure portal.
2.	Clone this repository
3.	Open the main scene, which you kind find in the root of the Assets folder
4.	Select the myInfo Script and enter following information from your Azure Portal:
o	Computer Vision key
Computer Vision Endpoint

5.	Set the project settings using the Mixed Reality Toolkit menu Configure - Apply Mixed Reality Project Settings.
6.	Open the Build Settings dialog File | Build Settings. Click on Add Open Scenes to add the main scene to your build.
7.	Build the UWP app from the Build Settings dialog. 
8.	Open the solution in the Builds folder with Visual Studio.
9.	Set the configuration in the top bar to Debug and x86. Select Device if your Hololens is connected with a cable, Remote Machine if it's connected over WiFi or the Hololens Emulator.
10.	Build the solution in Visual Studio and deploy to your Hololens.
