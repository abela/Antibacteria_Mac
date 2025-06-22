//
//  SoundManager.mm
//  PivotPoint
//
//  Created by Giorgi Abelashvili on 3/11/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "SoundManager.h"
#import "SimpleAudioEngine.h"
#import "GameSettingsManager.h"
#import "Utils.h"
#import "GameManager.h"
#import "GameSettingsManager.h"

@implementation SoundManager
//
@synthesize soundsAreStoped;
//
static SoundManager *_sharedSoundManager = nil;
//
+(SoundManager*)sharedSoundManager
{
    @synchronized([SoundManager class])
    {
        if (!_sharedSoundManager)
        {
            _sharedSoundManager = [[SoundManager alloc] init];
        }
        return _sharedSoundManager;
    }
}
//
+(void) initSounds
{
}
//
- (void)playSoundEffectForObject:(Figure*)object
{
}
//
//
- (void) playSoundEffectAtString:(NSString*)effect
{
    if([[GameSettingsManager sharedGameSettingsManager] sfxIsEnabled])
    {
        [[SimpleAudioEngine sharedEngine] playEffect:effect];
    }
}
//
- (void) setEffectsVolume:(float)value
{
	//[[SimpleAudioEngine sharedEngine] setEffectsVolume:value];
    underWaterThemePlayer.volume =value;
}
//
- (void) playBackgroundMusicAtString:(NSString*)filePath WithLoop:(BOOL)loop
{
    [[SimpleAudioEngine sharedEngine] playBackgroundMusic:filePath loop:YES];
}
//
- (void) setBackgroundMusicVolume:(float)value
{
    //
    // 0.0f to 1.0f
    //
    [[SimpleAudioEngine sharedEngine] setBackgroundMusicVolume:value];
}
//
- (void) stopBackgroundMusic
{
	[[SimpleAudioEngine sharedEngine] stopBackgroundMusic];
}
//
-(void) playUnderwaterThemeMusicAtString:(NSString *)path
{
    
}
//
- (void) stopUnderWaterThemMusic
{
    [underWaterThemePlayer stop];
    [underWaterThemePlayer release];
    underWaterThemePlayer = nil;
}
//
@end
