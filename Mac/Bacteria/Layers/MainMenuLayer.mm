//
//  MainMenuLayer.mm
//  Tanks
//
//  Created by Giorgi Abelashvili on 11/17/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

#import "MainMenuLayer.h"
#import "MainGameScene.h"
#import "GameManager.h"
#import "ChooseGameModeLayer.h"
#import "SettingsLayer.h"
#import "StoreLayer.h"
#import "AboutLayer.h"
#import "CharacterUpgradeLayer.h"
#import "GameCenterManager.h"

//#import "OpenFeint.h"

@interface MainMenuLayer (PrivateMethods)
- (void) startGame:(id)sender;
- (void) settings:(id)sender;
- (void) store:(id)sender;
- (void) characterUpgrade:(id)sender;
- (void) leaderBoards:(id)sender;
- (void) about:(id)sender;
- (void) quit:(id)sender;
@end

@implementation MainMenuLayer

//
+(CCScene*)scene
{
	CCScene *scene = [CCScene node];
	MainMenuLayer *layer = [MainMenuLayer node];
	[scene addChild: layer];
	return scene;
}
//
- (id) init
{
	if ((self = [super init]))
	{
		//create menu 
		[self createLayerMenu];
        self.isKeyboardEnabled = YES;
        self.isMouseEnabled = YES;
		return self;
	}
	//
	return nil;
}

- (void)createLayerMenu
{
	//
	[CCMenuItemFont setFontSize:20];
	[CCMenuItemFont setFontName:@"Helvetica"];
	//
	CCMenuItem *start = [CCMenuItemFont itemFromString:[[LocalizationManager sharedLocalizationManager]
                                                        getValueWithKey:_START_GAME] target:self selector:@selector(startGame:)];
    CCMenuItem *settings = [CCMenuItemFont itemFromString:[[LocalizationManager sharedLocalizationManager]
                                                           getValueWithKey:_GAME_OPTIONS] target:self selector:@selector(settings:)];
    CCMenuItem *leaderBoards = [CCMenuItemFont itemFromString:[[LocalizationManager sharedLocalizationManager]
                                                               getValueWithKey:_LEADERBOARDS]
                                                       target:self selector:@selector(leaderBoards:)];
    CCMenuItem *store = [CCMenuItemFont itemFromString:[[LocalizationManager sharedLocalizationManager]
                                                        getValueWithKey:_VITAMIN_STORE]
                                                target:self selector:@selector(store:)];
    CCMenuItem *characterUpgrade = [CCMenuItemFont itemFromString:[[LocalizationManager sharedLocalizationManager]
                                                                   getValueWithKey:_UPGRADE_CHARACTER]
                                                           target:self selector:@selector(characterUpgrade:)];
    CCMenuItem *about  = [CCMenuItemFont itemFromString:[[LocalizationManager sharedLocalizationManager]
                                                         getValueWithKey:_ABOUT]
                                                 target:self selector:@selector(about:)];
    CCMenuItem *quit = [CCMenuItemFont itemFromString:[[LocalizationManager sharedLocalizationManager]
                                                       getValueWithKey:_QUIT] target:self selector:@selector(quit:)];
	//
	menu = [CCMenu menuWithItems:start,characterUpgrade,store,settings,leaderBoards,about,quit,nil];
	//
	[menu alignItemsHorizontally];
	[self addChild:menu];
	//
	[start setPosition:ccp(0.0f,20.0f)];
    [characterUpgrade setPosition:ccp(0.0f,-20.0f)];
    [store setPosition:ccp(0.0f,-60.0f)];
    [leaderBoards setPosition:ccp(0.0f,-100.0f)];
    [settings setPosition:ccp(0.0f,-140.0f)];
    [about setPosition:ccp(0.0f,-180.0f)];
    [quit setPosition:ccp(0.0f,-220.0f)];
    //
}

//start game callback
- (void) startGame:(id)sender
{
    [[CCDirector sharedDirector] replaceScene:[ChooseGameModeLayer scene]];
}
//
// character upgrade
- (void) characterUpgrade:(id)sender
{
    [[CCDirector sharedDirector] replaceScene:[CharacterUpgradeLayer scene]];
}
//
// settings callback
-(void) settings:(id)sender
{
    [[CCDirector sharedDirector] replaceScene:[SettingsLayer scene]];
}
// stoer callback
- (void) store:(id)sender;
{
    [[CCDirector sharedDirector] replaceScene:[StoreLayer scene]];
}
//
-(void) leaderBoards:(id)sender
{
    // show gamecenter here
    [[GameCenterManager sharedManager] loadLeaderboards];
}
//
// about callback
- (void) about:(id)sender
{
    [[CCDirector sharedDirector] replaceScene:[AboutLayer scene]];
}
//
-(BOOL) ccKeyDown:(NSEvent *)event
{
    NSString *key = [event characters];
    unichar keyCode = [key characterAtIndex: 0];
    if(keyCode == ESC_KEY)
    {
        [self removeChild:currentLoadedLayer cleanup:YES];
        menu.visible = YES;
        currentLoadedLayer = nil;
    }
    return YES;
}
//
-(void) quit:(id)sender
{
    exit(0);
}
//
-(void) onExit
{
    [super onExit];
    self.isKeyboardEnabled = NO;
    self.isMouseEnabled = NO;
}
@end
