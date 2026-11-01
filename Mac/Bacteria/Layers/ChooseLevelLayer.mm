//
//  ChooseLevelLayer.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/26/13.
//
//
//
#import "ChooseLevelLayer.h"
#import "cocos2d.h"
#import "LevelManager.h"
#import "MainGameScene.h"
#import "ChooseGameModeLayer.h"
#import "Utils.h"
//
@interface ChooseLevelLayer (PrivateMethods)
-(void) enableChooseGameMode:(id)sender;
-(void) startLevel:(id)sender;
-(void) chooseGameMode:(int)spriteIndex;
-(void) createChooseGameModeMenu;
@end
//
@implementation ChooseLevelLayer
+(CCScene*)scene
{
	CCScene *scene = [CCScene node];
	ChooseLevelLayer *layer = [ChooseLevelLayer node];
	[scene addChild: layer];
	return scene;
}
//
- (id) init
{
	if ((self = [super init]))
	{
		//create levels menu
		[self createLayerMenu];
        // create game modes menu
        [self createChooseGameModeMenu];
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
    float aspectRatio = [[CoreSettings sharedCoreSettings] aspectRatio];
    CCLabelTTF *titleLabel = [CCLabelTTF labelWithString:[[LocalizationManager sharedLocalizationManager] getValueWithKey:_CHOOSE_LEVEL]
                                              dimensions:CGSizeMake(500*aspectRatio,80*aspectRatio)
                                              hAlignment:kCCTextAlignmentCenter
                                                fontName:@"Times New Roman"
                                                fontSize:60*aspectRatio];
    float x = [CCDirector sharedDirector].winSize.width / 2.0f;
    float y = [CCDirector sharedDirector].winSize.height  - 100.0f*aspectRatio;
    //
    titleLabel.position = ccp(x,y);
    titleLabel.color = ccc3(0, 255, 0);
    [self addChild:titleLabel];
    //
    float startX = -2.0*225.0f*aspectRatio;
    float startY = 1.5f*128.0f*aspectRatio;
    //
    float stepX = 100.0f*aspectRatio;
    float stepY = 100.0f*aspectRatio;
    //
    NSMutableArray *menuItemsArray = [[NSMutableArray alloc] init];
    //
    int levelTagCounter = 1;
    //
    for (int i = 0; i < 5; i++)
    {
        for (int j = 0; j < 10; j++)
        {
            //
            CCMenuItemImage *menuItem = nil;
            BOOL levelIsOpen = NO;
            //
            if([[LevelManager sharedLevelManager] levelIsUnlocked:levelTagCounter])
            {
                menuItem = [CCMenuItemImage itemWithNormalImage:@"vitamin.png"
                                                  selectedImage:@"vitamin.png"];
                menuItem.tag = levelTagCounter;
                levelIsOpen = YES;
                if (j == 9) {       // this is boss level
                    menuItem.color = ccc3(0, 128, 255);
                }
                //
                [menuItem setTarget:self selector:@selector(enableChooseGameMode:)];
                //
            }
            //
            else
            {
                menuItem = [CCMenuItemImage itemWithNormalImage:@"vitamin.png"
                                                  selectedImage:@"vitamin.png"];
                menuItem.opacity = 128;
                menuItem.tag = -1;
                levelIsOpen = NO;
                
                menuItem.tag = levelTagCounter;
                levelIsOpen = YES;
                if (j == 9) {       // this is boss level
                    menuItem.color = ccc3(0, 128, 255);
                }
                else menuItem.color = ccc3(255, 0, 0);
            }
            //
            menuItem.scale = 0.25f*aspectRatio;
            [menuItem setPosition:ccp(startX,startY)];
            [menuItemsArray addObject:menuItem];
            startX+=stepX;
            levelTagCounter++;
            //
        }
        //
        startY-=stepY;
        startX = -2.0*225.0f*aspectRatio;
        //
    }
    //
    menu = [CCMenu menuWithArray:menuItemsArray];
    [self addChild:menu];
    //
}
//
-(void) createChooseGameModeMenu
{
    //
    float aspectRatio = [[CoreSettings sharedCoreSettings] aspectRatio];
    //float height = [[CCDirector sharedDirector] winSize].height / 2.0f;
    float width  = [[CCDirector sharedDirector] winSize].width / 2.0f;
    //
    //
    CCLabelTTF *normalModeTitle = [CCLabelTTF labelWithString:[[LocalizationManager sharedLocalizationManager]
                                                               getValueWithKey:_NORMAL]
                                              dimensions:CGSizeMake(500*aspectRatio,40*aspectRatio)
                                              hAlignment:kCCTextAlignmentCenter
                                                fontName:@"Times New Roman"
                                                fontSize:30*aspectRatio];
    normalModeTitle.position = ccp(width - 300.0f * aspectRatio,180.0f * aspectRatio);
    [self addChild:normalModeTitle];
    //
    //
    // normal mode sprite
    CCSprite *normalModeSprite = [CCSprite spriteWithFile:@"BuyButton.png"];
    normalModeSprite.position = ccp(width - 300.0f * aspectRatio,100.0f * aspectRatio);
    [self addChild:normalModeSprite];
    //
    //
    //
    CCLabelTTF *hardModeTitle = [CCLabelTTF labelWithString:[[LocalizationManager sharedLocalizationManager]
                                                             getValueWithKey:_HARD]
                                                   dimensions:CGSizeMake(500*aspectRatio,40*aspectRatio)
                                                   hAlignment:kCCTextAlignmentCenter
                                                     fontName:@"Times New Roman"
                                                     fontSize:30*aspectRatio];
    hardModeTitle.position = ccp(width,180.0f * aspectRatio);
    [self addChild:hardModeTitle];
    //
    //
    CCSprite *hardModeModeSprite = [CCSprite spriteWithFile:@"BuyButton.png"];
    hardModeModeSprite.position = ccp(width,100.0f * aspectRatio);
    [self addChild:hardModeModeSprite];
    //
    //
    //
    CCLabelTTF *impossibleModeTitle = [CCLabelTTF labelWithString:[[LocalizationManager sharedLocalizationManager]
                                                                   getValueWithKey:_IMPOSSIBLE]
                                                 dimensions:CGSizeMake(500*aspectRatio,40*aspectRatio)
                                                 hAlignment:kCCTextAlignmentCenter
                                                   fontName:@"Times New Roman"
                                                   fontSize:30*aspectRatio];
    impossibleModeTitle.position = ccp(width + 300.0f * aspectRatio,180.0f * aspectRatio);
    [self addChild:impossibleModeTitle];
    //
    //
    CCSprite *impossibleModeModeSprite = [CCSprite spriteWithFile:@"BuyButton.png"];
    impossibleModeModeSprite.position = ccp(width + 300.0f * aspectRatio ,100.0f * aspectRatio);
    [self addChild:impossibleModeModeSprite];
    //
    //
    gameModesSprites.push_back(normalModeSprite);
    gameModesSprites.push_back(hardModeModeSprite);
    gameModesSprites.push_back(impossibleModeModeSprite);
    //
    int gameDifficulty = (int)[[GameSettingsManager sharedGameSettingsManager] gameDifficulty];
    [self chooseGameMode:gameDifficulty];
    //
}
//
-(void) chooseGameMode:(int)spriteIndex
{
    for (int i = 0; i<gameModesSprites.size(); i++) {
        gameModesSprites[i].color = ccc3(255, 255, 255);
    }
    gameModesSprites[spriteIndex].color = ccc3(255, 0, 0);
    GameDifficulty currentGameDifficulty = (GameDifficulty)spriteIndex;
    [[GameSettingsManager sharedGameSettingsManager] setGameDifficulty:currentGameDifficulty];
    [[GameSettingsManager sharedGameSettingsManager] saveGameSettings];
}
//
-(void)chooseGameModeMenuAnimationHasDone
{
    float x = 0.0f;
    float y = 0.0f;
    CCLabelTTF *normalLabel = [CCLabelTTF labelWithString:@"Normal"
                                              dimensions:CGSizeMake(500,40)
                                              hAlignment:kCCTextAlignmentCenter
                                                fontName:@"Times New Roman"
                                                fontSize:20];
    x = 120.0f;
    y = [CCDirector sharedDirector].winSize.height / 2.0f + 200.0f;
    //
    normalLabel.position = ccp(x,y);
    normalLabel.color = ccc3(0, 255, 0);
    [self addChild:normalLabel];
    //
    //
    //
    CCLabelTTF *hardLabel = [CCLabelTTF labelWithString:@"Hard"
                                               dimensions:CGSizeMake(500,40)
                                               hAlignment:kCCTextAlignmentCenter
                                                 fontName:@"Times New Roman"
                                                 fontSize:20];
    x = 120.0f;
    y = [CCDirector sharedDirector].winSize.height / 2.0f + 100.0f;
    //
    hardLabel.position = ccp(x,y);
    hardLabel.color = ccc3(0, 255, 0);
    [self addChild:hardLabel];
    //
    //
    //
    CCLabelTTF *impossibleLabel = [CCLabelTTF labelWithString:@"Impossible"
                                             dimensions:CGSizeMake(500,40)
                                             hAlignment:kCCTextAlignmentCenter
                                               fontName:@"Times New Roman"
                                               fontSize:20];
    x = 130.0f;
    y = [CCDirector sharedDirector].winSize.height / 2.0f;
    //
    impossibleLabel.position = ccp(x,y);
    impossibleLabel.color = ccc3(0, 255, 0);
    [self addChild:impossibleLabel];
}
//
-(void) enableChooseGameMode:(id)sender
{
    //
    CCMenuItemImage *image = (CCMenuItemImage*)sender;
    int tag = (int)image.tag;
    if (tag == -1) {
        return;
    }
    //
    GameDifficulty currentGameDifficulty = [[GameSettingsManager sharedGameSettingsManager] gameDifficulty];
    [[LevelManager sharedLevelManager] setLevelCounter:tag];
    [[CCDirector sharedDirector] replaceScene:[MainGameScene node]];
}
//
-(void) startLevel:(id)sender
{
    //
    CCMenuItemImage *image = (CCMenuItemImage*)sender;
    int tag = (int)image.tag - 100;
    gameMode = tag;
    //
    GameDifficulty currentGameDifficulty = (GameDifficulty)gameMode;
    [[GameSettingsManager sharedGameSettingsManager] setGameDifficulty:currentGameDifficulty];
    [[LevelManager sharedLevelManager] setLevelCounter:currentLevel];
    [[CCDirector sharedDirector] replaceScene:[MainGameScene node]];
    //
}
//
-(BOOL) ccMouseDown:(NSEvent *)event
{
    CGPoint mousePoints = [[CCDirector sharedDirector] convertEventToGL:event];
    for (int i = 0; i<gameModesSprites.size(); i++) {
        BOOL isInButton = [Utils pointInRect:mousePoints andRect:gameModesSprites[i].boundingBox];
        if (isInButton) {
            [self chooseGameMode:i];
        }
    }
    return YES;
}
//
-(BOOL) ccKeyDown:(NSEvent *)event
{
    NSString *key = [event characters];
    unichar keyCode = [key characterAtIndex: 0];
    if(keyCode == ESC_KEY)
    {
        [[CCDirector sharedDirector] replaceScene:[ChooseGameModeLayer scene]];
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
