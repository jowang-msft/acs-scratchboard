// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React, { useRef, useEffect, createRef, useState } from "react";
import {
    Image,
    SafeAreaView,
    StyleSheet,
    ScrollView,
    View,
    Text,
    StatusBar,
    Pressable,
    Linking,
} from "react-native";

import {
    Colors,
    Header,
} from "react-native/Libraries/NewAppScreen";

import { showNotification } from './Notifications';

import ACS = Azure.Communication.Calling.WindowsClient;

import MediaPlayerView from './MediaPlayerView';

let updateMediaSource: (arg0: string) => void;

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
        callAgentOptions.displayName = "RNWinRT Tester";

        var callAgent = await callClient.createCallAgentAsync(creds, callAgentOptions);

        var joinCallOptions = new ACS.JoinCallOptions();
        var incomingVideoOptions = new ACS.IncomingVideoOptions();
        incomingVideoOptions.streamKind = ACS.VideoStreamKind.remoteIncoming;
        joinCallOptions.incomingVideoOptions = incomingVideoOptions;

        var groupCallLocator = new ACS.GroupCallLocator("<RELACE_GROUP_ID>");
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
                                            updateMediaSource(uri.absoluteUri);
                                            console.info("*** ACS: " + "Incoming video received at " + uri.absoluteUri);
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
    const [streamUri, setStreamUri] = useState("https://content.jwplatform.com/manifests/yp34SRmf.m3u8");

    updateMediaSource = setStreamUri;

    useEffect(() => {
        testIncomingVideoAsync().then(() => {
            console.info("*** ACS: Joined the call group");
        });
      }, []);

    return (
        <>
            <StatusBar barStyle="dark-content" />
            <SafeAreaView style={styles.root}>
                <ScrollView
                    contentInsetAdjustmentBehavior="automatic"
                    style={styles.scrollView}>
                    <Header />
                    <View style={styles.body}>
                        <View style={styles.sectionContainer}>
                            <Text style={styles.sectionTitle}>Windows.UI.Notifications Example</Text>
                            <View style={{ flexDirection: 'row', flexWrap: 'wrap', alignItems: 'center' }}>
                                <Text style={[{ paddingRight: 10 }, styles.sectionDescription]}>Click the button to show a notification: </Text>
                                <Pressable style={styles.sectionDescriptionButton} onPress={() => {
                                    showNotification({
                                        template: Windows.UI.Notifications.ToastTemplateType.toastImageAndText01,
                                        // The template schema can be found at https://docs.microsoft.com/previous-versions/windows/apps/hh761494(v=win.10)
                                        text: "hello world",
                                        image: {
                                            src: "https://microsoft.github.io/react-native-windows/img/header_logo.svg",
                                            alt: "React logo",
                                        }
                                    });
                                }}>
                                    <Text style={styles.sectionDescriptionButtonText}>Press</Text>
                                </Pressable>
                            </View>
                        </View>
                    </View>
                    <View style={styles.body}>
                        <View style={styles.sectionContainer}>
                            <Text style={styles.sectionTitle}>Incoming video rendered through native ACS Windows SDK</Text>
                            <SafeAreaView style={styles.App}>
                                <MediaPlayerView uri={streamUri} />
                            </SafeAreaView>
                        </View>
                    </View>
                </ScrollView>
            </SafeAreaView>
        </>
    );
};

const styles = StyleSheet.create({
    scrollView: {
        backgroundColor: Colors.white,
    },
    engine: {
        position: "absolute",
        right: 0,
    },
    body: {
        backgroundColor: Colors.white,
    },
    root: {
        backgroundColor: Colors.white,
        flex: 1,
    },
    sectionContainer: {
        marginTop: 32,
        paddingHorizontal: 24,
    },
    sectionTitle: {
        fontSize: 24,
        fontWeight: "600",
        color: Colors.black,
    },
    sectionDescription: {
        marginTop: 8,
        fontSize: 18,
        fontWeight: "400",
        color: Colors.dark,
    },
    sectionDescriptionButton: {
        alignItems: 'center',
        justifyContent: 'center',
        paddingVertical: 4,
        paddingHorizontal: 24,
        borderRadius: 1,
        backgroundColor: Colors.light,
    },
    sectionDescriptionButtonText: {
        fontSize: 18,
        fontWeight: "400",
        color: Colors.dark,
    },
    highlight: {
        fontWeight: "700",
    },
    footer: {
        color: Colors.dark,
        fontSize: 12,
        fontWeight: "600",
        padding: 4,
        paddingRight: 12,
        textAlign: "right",
    },
});

export default App;
