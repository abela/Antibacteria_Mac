//
//  SoundManager.h
//  PivotPoint
//
//  Created by Giorgi Abelashvili on 3/11/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "Figure.h"
#import "SimpleAudioEngine.h"

@interface SoundManager : NSObject {

    AVAudioPlayer *underWaterThemePlayer;
	//
    BOOL soundsAreStoped;
    //
}
//
@property (nonatomic,assign) BOOL soundsAreStoped;
//
+(SoundManager*)sharedSoundManager;
- (void) setEffectsVolume:(float)value;
- (void) setBackgroundMusicVolume:(float)value;
- (void) playBackgroundMusicAtString:(NSString*)filePath WithLoop:(BOOL)loop;
- (void) playSoundEffectAtString:(NSString*)effect;
- (void) stopBackgroundMusic;
- (void) playUnderwaterThemeMusicAtString:(NSString*)path;
- (void) stopUnderWaterThemMusic;
@end
