//
//  GameSettingsManager.mm
//  Turtle
//
//  Created by Giorgi Abelashvili on 5/22/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "GameSettingsManager.h"
#import "PlistAction.h"
#import "SoundManager.h"
#import "NSData+AES256.h"

//
#define IS_FIRST_TIME                   @"isfirsttime"
#define GAME_DIFFICULTY                 @"gamemode"
#define SOUND_ENALED                    @"soundenabled"
#define SFXENABLED_KEY                  @"sfxenabled"
#define OPENED_GAME_MODES_KEY           @"openedgamemodes"
#define CONTROL_TYPE                    @"controltype"
#define IS_AUTOFIRE_ENABLED             @"isautofireEnabled"
#define VITAMINS_COUNT                  @"vitamins"
//
@implementation GameSettingsManager
@synthesize soundIsEnabled;
@synthesize sfxIsEnabled;
@synthesize gameDifficulty;
@synthesize openedGameModes;
@synthesize controlType;
@synthesize isAutoFireEnabled;
@synthesize vitaminsCount;
//
static GameSettingsManager *_sharedGameSettingsManager = nil;
//
+(GameSettingsManager*)sharedGameSettingsManager
{
    @synchronized(self)
    {
        if(!_sharedGameSettingsManager)
            _sharedGameSettingsManager = [[GameSettingsManager alloc] init];
        return _sharedGameSettingsManager;
    }
}
//
-(id)init
{
    //
	//
	if((self = [super init]))
	{
		//
		[self loadGameSettings];
		return self;
	}
	//
	else return nil;
}
//
-(void) loadGameSettings
{
    //
    BOOL valid = NO;
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    //int isFirstTime = [[defaults secureObjectForKey:IS_FIRST_TIME valid:&valid] intValue];
    int isFirstTime = [[defaults objectForKey:IS_FIRST_TIME] intValue];
    
//    NSData *soundEnabledData = [NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithInt:9]];
//    NSData *data = [soundEnabledData AES256Encrypt];
//    NSNumber *number = [NSKeyedUnarchiver unarchiveObjectWithData:[data AES256Decrypt]];
//    
//    NSLog(@"%d", [number intValue]);
    
    //
    if (isFirstTime == 0) {
        //
        sfxIsEnabled = YES;
        soundIsEnabled = YES;
        //
        gameDifficulty = kNormal;
        openedGameModes = 1;
        controlType = kKeyBoardAndMouse;
        isAutoFireEnabled = NO;
        isFirstTime = 1;
        vitaminsCount = 0;
        //
        //[defaults setSecureObject:[NSNumber numberWithBool:YES] forKey:SOUND_ENALED];
        NSData *soundData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithBool:YES]] AES256Encrypt];
        [defaults setObject:soundData forKey:SOUND_ENALED];
        
        //[defaults setSecureObject:[NSNumber numberWithBool:YES] forKey:SFXENABLED_KEY];
        NSData *soundSFXEData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithBool:YES]] AES256Encrypt];
        [defaults setObject:soundSFXEData forKey:SFXENABLED_KEY];
        
        //[defaults setSecureObject:[NSNumber numberWithInt:gameDifficulty] forKey:GAME_DIFFICULTY];
        NSData *gameDifficultyData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithInt:gameDifficulty]] AES256Encrypt];
        [defaults setObject:gameDifficultyData forKey:GAME_DIFFICULTY];
        
        //[defaults setSecureObject:[NSNumber numberWithInt:isFirstTime] forKey:IS_FIRST_TIME];
        [defaults setObject:[NSNumber numberWithInt:isFirstTime] forKey:IS_FIRST_TIME];
        
        //[defaults setSecureObject:[NSNumber numberWithInt:openedGameModes] forKey:OPENED_GAME_MODES_KEY];
        NSData *gameModesData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithInt:openedGameModes]] AES256Encrypt];
        [defaults setObject:gameModesData forKey:OPENED_GAME_MODES_KEY];
        
        //[defaults setSecureObject:[NSNumber numberWithInt:controlType] forKey:CONTROL_TYPE];
        NSData *controlTypeData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithInt:controlType]] AES256Encrypt];
        [defaults setObject:controlTypeData forKey:CONTROL_TYPE];
        
        //[defaults setSecureObject:[NSNumber numberWithBool:isAutoFireEnabled] forKey:IS_AUTOFIRE_ENABLED];
        NSData *autoFireData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithBool:isAutoFireEnabled]] AES256Encrypt];
        [defaults setObject:autoFireData forKey:IS_AUTOFIRE_ENABLED];
        
        //[defaults setSecureInteger:vitaminsCount forKey:VITAMINS_COUNT];
        NSData *vitaminsData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithInt:vitaminsCount]] AES256Encrypt];
        [defaults setObject:vitaminsData forKey:VITAMINS_COUNT];
        
        [defaults synchronize];
        //
    }
    //
    //
    else {
        //soundIsEnabled = [[defaults secureObjectForKey:SOUND_ENALED valid:&valid] boolValue];
        NSData *soundData = [defaults objectForKey:SOUND_ENALED];
        soundIsEnabled = [[NSKeyedUnarchiver unarchiveObjectWithData:[soundData AES256Decrypt]] boolValue];
        
        //sfxIsEnabled = [[defaults secureObjectForKey:SFXENABLED_KEY valid:&valid] boolValue];
        NSData *soundSFXEData = [defaults objectForKey:SFXENABLED_KEY];
        sfxIsEnabled = [[NSKeyedUnarchiver unarchiveObjectWithData:[soundSFXEData AES256Decrypt]] boolValue];
        
        //gameDifficulty = (GameDifficulty)[[defaults secureObjectForKey:GAME_DIFFICULTY valid:&valid] intValue];
        NSData *gameDifficultyData = [defaults objectForKey:GAME_DIFFICULTY];
        gameDifficulty = (GameDifficulty)[[NSKeyedUnarchiver unarchiveObjectWithData:[gameDifficultyData AES256Decrypt]] intValue];
        
        //openedGameModes = [[defaults secureObjectForKey:OPENED_GAME_MODES_KEY valid:&valid] intValue];
        NSData *gameModesData = [defaults objectForKey:OPENED_GAME_MODES_KEY];
        openedGameModes = [[NSKeyedUnarchiver unarchiveObjectWithData:[gameModesData AES256Decrypt]] intValue];
        
        //controlType = (CharacterMoveType)[[defaults secureObjectForKey:CONTROL_TYPE valid:&valid] intValue];
        NSData *controlTypeData = [defaults objectForKey:CONTROL_TYPE];
        controlType = (CharacterMoveType)[[NSKeyedUnarchiver unarchiveObjectWithData:[controlTypeData AES256Decrypt]] intValue];
        
        //isAutoFireEnabled = [[defaults secureObjectForKey:IS_AUTOFIRE_ENABLED valid:&valid] boolValue];
        NSData *autoFireData = [defaults objectForKey:IS_AUTOFIRE_ENABLED];
        isAutoFireEnabled = [[NSKeyedUnarchiver unarchiveObjectWithData:[autoFireData AES256Decrypt]] boolValue];
        
        //vitaminsCount = (int)[defaults secureIntegerForKey:VITAMINS_COUNT valid:&valid];
        NSData *vitaminsData = [defaults objectForKey:VITAMINS_COUNT];
        vitaminsCount = [[NSKeyedUnarchiver unarchiveObjectWithData:[vitaminsData AES256Decrypt]] intValue];
    }
}
//
-(void) saveGameSettings
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    
    //[defaults setSecureObject:[NSNumber numberWithBool:soundIsEnabled] forKey:SOUND_ENALED];
    NSData *soundData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithBool:soundIsEnabled]] AES256Encrypt];
    [defaults setObject:soundData forKey:SOUND_ENALED];
    
    //[defaults setSecureObject:[NSNumber numberWithBool:sfxIsEnabled] forKey:SFXENABLED_KEY];
    NSData *soundSFXEData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithBool:sfxIsEnabled]] AES256Encrypt];
    [defaults setObject:soundSFXEData forKey:SFXENABLED_KEY];
    
    //[defaults setSecureObject:[NSNumber numberWithInt:gameDifficulty] forKey:GAME_DIFFICULTY];
    NSData *gameDifficultyData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithInt:gameDifficulty]] AES256Encrypt];
    [defaults setObject:gameDifficultyData forKey:GAME_DIFFICULTY];
    
    //[defaults setSecureObject:[NSNumber numberWithInt:openedGameModes] forKey:OPENED_GAME_MODES_KEY];
    NSData *gameModesData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithInt:openedGameModes]] AES256Encrypt];
    [defaults setObject:gameModesData forKey:OPENED_GAME_MODES_KEY];
    
    //[defaults setSecureObject:[NSNumber numberWithInt:controlType] forKey:CONTROL_TYPE];
    NSData *controlTypeData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithInt:controlType]] AES256Encrypt];
    [defaults setObject:controlTypeData forKey:CONTROL_TYPE];
    
    //[defaults setSecureObject:[NSNumber numberWithBool:isAutoFireEnabled] forKey:IS_AUTOFIRE_ENABLED];
    NSData *autoFireData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithBool:isAutoFireEnabled]] AES256Encrypt];
    [defaults setObject:autoFireData forKey:IS_AUTOFIRE_ENABLED];
    
    //[defaults setSecureInteger:vitaminsCount forKey:VITAMINS_COUNT];
    NSData *vitaminsData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithInt:vitaminsCount]] AES256Encrypt];
    [defaults setObject:vitaminsData forKey:VITAMINS_COUNT];
    
    [defaults synchronize];
}
//
@end
