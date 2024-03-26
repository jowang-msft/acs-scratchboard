// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React, { useRef, useEffect, useState } from "react";
import {
    View,
    Text,
} from "react-native";

import {
    MediaPlayerElement,
  } from 'react-native-xaml';
  

import { showNotification } from './Notifications';

import ACS = Azure.Communication.Calling.WindowsClient;

// import MediaPlayerView from './MediaPlayerView';

let updateIncomingVideoSource: (arg0: string) => void;
let updateOutgoingVideoSource: (arg0: string) => void;

async function testIncomingVideoAsync(): Promise<void> {
    console.info("*** ACS: Entering testIncomingVideoAsync");

    try {
        var callClientOptions = new ACS.CallClientOptions();
        var callClient = new ACS.CallClient(callClientOptions);
        var deviceManager = await callClient.getDeviceManagerAsync();
        var cameras = deviceManager.cameras;
        console.info("*** ACS: " + cameras[0].name);

        var tokenRefreshOptions = new ACS.CallTokenRefreshOptions(false);
        var creds = new ACS.CallTokenCredential(
            "<REPLACE_WITH_ACS_TOKEN>",
            tokenRefreshOptions);

        var callAgentOptions = new ACS.CallAgentOptions();
        callAgentOptions.displayName = "RNW Tester";

        var callAgent = await callClient.createCallAgentAsync(creds, callAgentOptions);

        var joinCallOptions = new ACS.JoinCallOptions();
        var incomingVideoOptions = new ACS.IncomingVideoOptions();
        incomingVideoOptions.streamKind = ACS.VideoStreamKind.remoteIncoming;
        joinCallOptions.incomingVideoOptions = incomingVideoOptions;

        var groupCallLocator = new ACS.GroupCallLocator("<REPLACE_WITH_GROUP_ID>");
        var call = await callAgent.joinAsync(groupCallLocator, joinCallOptions);

        call.addEventListener('remoteparticipantsupdated', 
            (sender: any, args: ACS.ParticipantsUpdatedEventArgs) => {
                console.info("*** ACS: " + "Participant updated!");

                var remoteParticipant = args.addedParticipants[0];
                if (remoteParticipant != null)
                {
                    console.info("*** ACS: " + "Participant - " + remoteParticipant.identifier.rawId);
                }

                remoteParticipant.addEventListener('videostreamstatechanged', 
                    async (sender: any, args: ACS.VideoStreamStateChangedEventArgs) =>{
                        console.info("*** ACS: " + "VideoStreamStateChanged");
                        var stream = args.stream;
                        if (stream.direction == ACS.StreamDirection.incoming) {
                            switch(stream.state) {
                                case ACS.VideoStreamState.available:
                                    console.info("*** ACS: " + "IncomingVideoStream available");
                                    switch(stream.kind) {
                                        case ACS.VideoStreamKind.remoteIncoming:
                                            var remoteIncomingVideoStream = stream as unknown as ACS.RemoteIncomingVideoStream;
                                            var uri = await remoteIncomingVideoStream.startPreviewAsync();
                                            updateIncomingVideoSource(uri.absoluteUri);
                                            console.info("*** ACS: " + "Incoming video received at " + uri.absoluteUri);
                                            break;
                                    }
                                    break;
                            }
                        }
                        else if (stream.direction == ACS.StreamDirection.outgoing) {
                            switch(stream.state) {
                                case ACS.VideoStreamState.available:
                                    console.info("*** ACS: " + "OutgoingVideoStream available");
                                    switch(stream.kind) {
                                        case ACS.VideoStreamKind.localOutgoing:
                                            var localOutgoingVideoStream = stream as unknown as ACS.LocalOutgoingVideoStream;
                                            var uri = await localOutgoingVideoStream.startPreviewAsync();
                                            updateOutgoingVideoSource(uri.absoluteUri);
                                            console.info("*** ACS: " + "Outgoing video received at " + uri.absoluteUri);
                                            break;
                                    }
                                    break;
                            }

                        }
                    });
            });
    }
    catch (e)
    {
        console.error(`ACS SDK failed due to ${e}`);
    }
}

const App = () => {
    const [incomingVideoStreamUri, setIncomingVideoStreamUri] = useState("http://sample.vodobox.net/skate_phantom_flex_4k/skate_phantom_flex_4k.m3u8");
    const [outgoingVideoStreamUri, setOutgoingVideoStreamUri] = useState("https://cdn.flowplayer.com/a30bd6bc-f98b-47bc-abf5-97633d4faea0/hls/de3f6ca7-2db3-4689-8160-0f574a5996ad/playlist.m3u8");

    updateIncomingVideoSource = setIncomingVideoStreamUri;
    updateOutgoingVideoSource = setOutgoingVideoStreamUri;

    useEffect(() => {
        testIncomingVideoAsync().then(() => {
            console.info("*** ACS: Joined the call group");
        });
      }, []);

      return (
        <View>
          <View style={{width: 400, height: 300, backgroundColor: 'steelblue'}}>
            <View>
                <Text style={{fontSize: 18, textAlign: 'center'}}>
                    Outgoing video stream
                </Text>
            </View>
            <MediaPlayerElement source={outgoingVideoStreamUri} autoPlay="true" />
          </View>
          <View style={{width: 400, height: 300, backgroundColor: 'powderblue'}}>
            <View>
                <Text style={{fontSize: 18, textAlign: 'center'}}>
                    Incoming video stream
                </Text>            
            </View>
            <MediaPlayerElement source={incomingVideoStreamUri} autoPlay="true" />
          </View>
        </View>
      );
};

export default App;
