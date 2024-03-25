# ACS Calling SDK React Native WinRT Sample project

This sample code is created to demonstrate how to take advantage of highly performant native ACS Calling Windows SDK in the context of programing in React by leveraging React Native, and supporting tools such as React Native WinRT.

>**Note: This sample here is not ready to build, you will need to follow the standard React Native Windows setup proceture to initialize the dev and runtime environment.**

### Resources
Before diving into ACS native SDK integration, make sure get familiarized with the the basics covered in the following resources

1. What is any why [React Native](https://reactnative.dev/).
2. What is [React Native for Windows](https://microsoft.github.io/react-native-windows/).
3. What is and why [React Native WinRT](https://github.com/microsoft/react-native-winrt/tree/main).
4. [Quickstart sample for ACS Calling SDK](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/voice-video-calling/getting-started-with-calling?tabs=uwp&pivots=platform-windows).


### Sample components

The sample is made of 3 parts:
1. A standalone UWP app that hosts the JS runtime engine and and custom components.
   ```
   This can be found as part of the [React Native WinRT sample](https://github.com/microsoft/react-native-winrt/tree/main/samples/RNWinRTTestApp/windows), with the additional reference to the MediaPlayerModule and added support for skype source handler.
   ```
2. The standard WinRTTurboModule added by initial setup to enable React Native WinRT. See React Native WinRT [developer usage](https://github.com/microsoft/react-native-winrt/blob/main/docs/USAGE.md). This module automatically projects WinRT interfaces and types into .ts along with the supporting native implementations - a huge saving to developers when integrating new WinRT API in React Native project.
3. MediaPlayerModule
   ```
   The ACS Native SDK for Windows (which this sample relies on) mandates that video streams must be associated with a Windows.UI.Xaml.Controls.MediaPlayerElement, which is not natively supported by React Native for Windows. MediaPlayerModule is a cusome module created to expose this UI component to the JS world.
   ```
   The implementation was inspired by [stackoverflow post](https://stackoverflow.com/questions/69408062/react-native-windows-native-ui-component-wont-display). And a detailed projection of [datetimepicker](https://github.com/react-native-datetimepicker/datetimepicker/tree/master).

### Customizations

In order to render the video stream from the remote participant, it is crutial to update the Package.appxmanifest file to include this section:

   ```
   <Extensions>
    <Extension Category="windows.activatableClass.inProcessServer">
      <InProcessServer>
        <Path>RtmMvrUap.dll</Path>
        <ActivatableClass ActivatableClassId="VideoN.VideoSchemeHandler" ThreadingModel="both" />
      </InProcessServer>
    </Extension>
   </Extensions> 
   ```

WinRTTurboModule only references standard WinRT namespace and generates the JS projections for them, therefore we need to manually add the reference to Azure.Communication.Calling.WindowsClient.winmd, which you can find from the SDK nuget.
>**Note: Azure.Communication.Calling.WindowsClient.winmd nuget isn't ready for direct reference from UWP C++ project, you will have to manually extract the winmd from the nuget package.

RNWinRTTestApp needs to manually updated to take a dependency on MediaPlayerModule project, and insert these two lines in App.cpp:
```
#include <winrt/MediaPlayerModule.h>
```
and 
```
    App::App() noexcept
    {
        :
        :
        PackageProviders().Append(winrt::MediaPlayerModule::ReactPackageProvider());
        :
    }
```

>**Note: Raw video frames are not currently supported due to the lack of `Blitable` surface exposed by React (or my limited knowledge in React), as well as a more performant solution to share byte arrays without going through base64 encoding. This is a work in progress.