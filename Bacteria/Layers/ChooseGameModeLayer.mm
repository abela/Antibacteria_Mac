//
//  ChooseGameModeLayer.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import "ChooseGameModeLayer.h"
#import "cocos2d.h"
#import "MainGameScene.h"
#import "GameModeManager.h"
//
#import "ChooseLevelLayer.h"
#import "MainMenuLayer.h"
//
@interface ChooseGameModeLayer (PrivateMethods)
-(void) compaign:(id)sender;
-(void) takeTheFlag:(id)sender;
-(void) rush:(id)sender;
-(void) peaceWalker:(id)sender;
-(void) armagedon:(id)sender;
@end
//
@implementation ChooseGameModeLayer
//
+(CCScene*)scene
{
	CCScene *scene = [CCScene node];
	ChooseGameModeLayer *layer = [ChooseGameModeLayer node];
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
        self.isMouseEnabled = YES;
        self.isKeyboardEnabled = YES;
		return self;
	}
	//
	return nil;
}
//
-(void) createLayerMenu
{
    //
    CCLabelTTF *titleLabel = [CCLabelTTF labelWithString:[[LocalizationManager sharedLocalizationManager] getValueWithKey:_CHOOSE_GAME]
                                              dimensions:CGSizeMake(500,40)
                                              hAlignment:kCCTextAlignmentCenter
                                                fontName:@"Times New Roman"
                                                fontSize:40];
    float x = [CCDirector sharedDirector].winSize.width / 2.0f;
    float y = [CCDirector sharedDirector].winSize.height / 2.0f + 100.0f;
    //
    titleLabel.position = ccp(x,y);
    titleLabel.color = ccc3(128, 0, 0);
    [self addChild:titleLabel];
    //
	[CCMenuItemFont setFontSize:20];
	[CCMenuItemFont setFontName:@"Helvetica"];
	//
	CCMenuItem *compaign = [CCMenuItemFont itemFromString:[[LocalizationManager sharedLocalizationManager]
                                                           getValueWithKey:_COMPAIGN]
                                                   target:self selector:@selector(compaign:)];
    CCMenuItem *takeTheFlag = [CCMenuItemFont itemFromString:[[LocalizationManager sharedLocalizationManager]
                                                              getValueWithKey:_CAPTURE_FLAG]
                                                      target:self selector:@selector(takeTheFlag:)];
    CCMenuItem *rush = [CCMenuItemFont itemFromString:[[LocalizationManager sharedLocalizationManager]
                                                       getValueWithKey:_RUSH]
                                               target:self selector:@selector(rush:)];
    CCMenuItem *peaceWalker  = [CCMenuItemFont itemFromString:[[LocalizationManager sharedLocalizationManager]
                                                               getValueWithKey:_PEACE_WALKER]
                                                       target:self selector:@selector(peaceWalker:)];
    CCMenuItem *armagedon  = [CCMenuItemFont itemFromString:@"Armagedon" target:self selector:@selector(armagedon:)];
	//
	menu = [CCMenu menuWithItems:compaign,takeTheFlag,rush,peaceWalker, nil];
	//
	[menu alignItemsHorizontally];
	[self addChild:menu];
	//
	[compaign setPosition:ccp(0.0f,20.0f)];
    [takeTheFlag setPosition:ccp(0.0f,-20.0f)];
    [rush setPosition:ccp(0.0f,-60.0f)];
    [peaceWalker setPosition:ccp(0.0f,-100.0f)];
    [armagedon setPosition:ccp(0.0f,-140.0f)];
    //
}
//
-(void) compaign:(id)sender
{
    //
    [[GameModeManager sharedGameModeManager] setCurrentGameModeType:kCompaignMode];
    [[CCDirector sharedDirector] replaceScene:[ChooseLevelLayer node]];
}
//
-(void) takeTheFlag:(id)sender
{
    [[GameModeManager sharedGameModeManager] setCurrentGameModeType:kTakeTheFlagMode];
    [[CCDirector sharedDirector] replaceScene:[MainGameScene node]];
}
//
-(void) rush:(id)sender
{
    [[GameModeManager sharedGameModeManager] setCurrentGameModeType:kRushMode];
    [[CCDirector sharedDirector] replaceScene:[MainGameScene node]];
}
//
-(void) peaceWalker:(id)sender
{
    [[GameModeManager sharedGameModeManager] setCurrentGameModeType:kPeaceWalkerMode];
    [[CCDirector sharedDirector] replaceScene:[MainGameScene node]];
}
//
-(void) armagedon:(id)sender
{
    [[GameModeManager sharedGameModeManager] setCurrentGameModeType:kArmagedonMode];
    [[CCDirector sharedDirector] replaceScene:[MainGameScene node]];
}
//
-(BOOL) ccKeyDown:(NSEvent *)event
{
    NSString *key = [event characters];
    unichar keyCode = [key characterAtIndex: 0];
    if(keyCode == ESC_KEY)
    {
        [[CCDirector sharedDirector] replaceScene:[MainMenuLayer scene]];
    }
    return YES;
}
//
-(void) onExit
{
    [menu removeFromParentAndCleanup:YES];
    menu = nil;
    self.isKeyboardEnabled = NO;
    self.isMouseEnabled = NO;
}
//
//
@end
