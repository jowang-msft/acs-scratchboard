import React from 'react';

import { StyleSheet, View} from 'react-native';
import { requireNativeComponent } from 'react-native';

type MediaPlayerViewProps = {
    uri: string;
  };

const MediaPlayerWindows = requireNativeComponent("MediaPlayerView")

const MediaPlayerView = (props : MediaPlayerViewProps) => {
  return(
    <View style={styles.container}>
      <MediaPlayerWindows source={props.uri} />
    </View>
  )
}

const styles = StyleSheet.create({
  container: {
    flex:1,
    justifyContent:'center',
    alignItems:'center',
  }
})

export default MediaPlayerView