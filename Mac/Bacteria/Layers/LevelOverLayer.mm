//
//  MainGameOverLayer.m
//  Tanks
//
//  Created by Giorgi Abelashvili on 11/17/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

#import "LevelOverLayer.h"
#import "GameManager.h"
#import "ScoreManager.h"
#import "MainGameLayer.h"
#import "MainMenuScene.h"
#import "LevelManager.h"
#import "Level.h"
#import "GameModeManager.h"
#import "GameModeManager.h"
#import "GameSettingsManager.h"

@interface  LevelOverLayer(PrivateMethods)
-(void) initHeartz;
-(void) reFillSpecialAbilitieBarTimer:(NSTimer*)timer;
-(void) bonusIsOver;
//
-(void) showGetReadyLabel:(NSTimer*)timer;
-(void) resumeGame:(id)sender;
-(void) restartGame:(id)sender;
-(void) levelSelect:(id)sender;
-(void) quit:(id)sender;
@end
//
@implementation LevelOverLayer

@synthesize mainGameLayerdelegate;

static LevelOverLayer *_sharedLevelOverLayer = nil;
//
+(LevelOverLayer*)sharedLevelOverLayer
{
    //
	@synchronized([CCDirector class])
	{
        //
		if (!_sharedLevelOverLayer) 
		{
            //
			_sharedLevelOverLayer = [[LevelOverLayer alloc] init];
            //
        }
		//
		return _sharedLevelOverLayer;
	}
    //
}
//
- (id) init
{
    //
	if(self == [super init]) 
	{
		//
        aspectRatio = [[CoreSettings sharedCoreSettings] aspectRatio];
		self.isTouchEnabled = YES;
        //
		return self;
        //
    }
    //
	return nil;
}
//
-(void) onExit
{
    [[CCTextureCache sharedTextureCache] removeUnusedTextures];
}
//
- (id) initWithGameLayer:(MainGameLayer*)g
{
    gameLayer = g;
    //
    [self initLevelOverLayer];
    //
    return self;
}
//
-(void) initLevelOverLayer
{
    //
    showBonusStringLabel = [CCLabelTTF labelWithString:@""
                                            dimensions:CGSizeMake(200,50)
                                            hAlignment:kCCTextAlignmentCenter
                                              fontName:@"MarkerFelt-Thin"
                                              fontSize:40*aspectRatio];
    //
    showBonusStringLabel.position = ccp([CCDirector sharedDirector].winSize.width/2.0f,
                                        [CCDirector sharedDirector].winSize.height / 2.0f +
                                        [CCDirector sharedDirector].winSize.height / 4.0f);
    //
    [self addChild:showBonusStringLabel];
    //
    //
    slowMoSprite = [CCSprite spriteWithFile:@"SlowMotionBc.png"];
    slowMoSprite.position = ccp([CCDirector sharedDirector].winSize.width / 2.0f,
                                [CCDirector sharedDirector].winSize.height / 2.0f);
    slowMoSprite.scale = 1.5f*aspectRatio;
    [self addChild:slowMoSprite];
    slowMoSprite.opacity = 0;
    //
    heartsArray = [[NSMutableArray alloc] init];
    //
    scoreCounter = 0;
    //
    NSString *scoreStr = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_SCORE];
    scoreStr = [scoreStr stringByAppendingString:@": 0"];
    //
    scoreCounter = [CCLabelTTF labelWithString:scoreStr
                                    dimensions:CGSizeMake(300,50)
                                    hAlignment:kCCTextAlignmentLeft
                                      fontName:@"MarkerFelt-Thin"
                                      fontSize:40*aspectRatio];
    //
    float x = 35.0f*aspectRatio;
    //
    float y = [CCDirector sharedDirector].winSize.height - 20.0f*aspectRatio;
    scoreCounter.position = ccp(x,y);
    scoreCounter.anchorPoint = ccp(0.0f,1.0f);
    scoreCounter.scale = 0.9f*aspectRatio;
    [self addChild:scoreCounter];
    //
    showTimer = [CCLabelTTF labelWithString:@"1 : 20"
                                 dimensions:CGSizeMake(200,50)
                                 hAlignment:kCCTextAlignmentCenter
                                   fontName:@"MarkerFelt-Thin"
                                   fontSize:40*aspectRatio];
    //
    y = [CCDirector sharedDirector].winSize.height - 40.0f*aspectRatio;
    x = [[CCDirector sharedDirector] winSize].width - 350.0f*aspectRatio;
    showTimer.position = ccp(x,y);
    showTimer.scale = 0.9f*aspectRatio;
    [self addChild:showTimer];
    //
    
    levelHasWonOrNotLabel = [CCLabelTTF labelWithString:scoreStr
                                             dimensions:CGSizeMake(500,50)
                                             hAlignment:kCCTextAlignmentCenter
                                               fontName:@"MarkerFelt-Thin"
                                               fontSize:40*aspectRatio];
    //
    levelHasWonOrNotLabel.position = ccp([CCDirector sharedDirector].winSize.width/2.0f,
                                         [CCDirector sharedDirector].winSize.height/1.5f);
    //
    levelHasWonOrNotLabel.scale = 2.0f*aspectRatio;
    [self addChild:levelHasWonOrNotLabel];
    //
    // init heartz
    [self initHeartz];
    // init boss life
    [self createSpecialAbilitieBar:YES];
    //
    if([[[LevelManager sharedLevelManager] currentLevel] number] % BOSS_LEVEL_CREATE_CONST == 0)
    {
        scoreCounter.opacity = 0;
        showTimer.opacity = 0;
    }
    //
    startSlowMotionFade = NO;
    //
    //
    x = 35.0f*aspectRatio;
    y = 65.0f*aspectRatio;
    //
    NSString *vitaminsStr = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_VITAMINS];
    vitaminsStr = [vitaminsStr stringByAppendingString:[NSString stringWithFormat:@": %d",[[GameSettingsManager
                                                                                            sharedGameSettingsManager] vitaminsCount]]];
    //
    vitaminsLabel = [CCLabelTTF labelWithString:vitaminsStr
                                     dimensions:CGSizeMake(400,50)
                                     hAlignment:kCCTextAlignmentLeft
                                       fontName:@"MarkerFelt-Thin"
                                       fontSize:40*aspectRatio];
    //
    vitaminsLabel.anchorPoint = ccp(0.0f,1.0f);
    vitaminsLabel.position = ccp(x,y);
    //
    vitaminsLabel.scale = 0.9f*aspectRatio;
    [self addChild:vitaminsLabel];
    //
    //
}
//
-(void) initHeartz
{
    //
    float startHeartX = [[CCDirector sharedDirector] winSize].width - 5.5*scoreCounter.position.x;
    float startHeartY = [CCDirector sharedDirector].winSize.height - 40.0f*aspectRatio;
    for (int i =0;i<3;i++)
    {
        CCSprite *heartSprite = [CCSprite spriteWithTexture:[[CCTextureCache sharedTextureCache] addImage:@"heart.png"]];
        heartSprite.scale = 0.5f*aspectRatio;
        heartSprite.position = ccp(startHeartX,startHeartY);
        [self addChild:heartSprite z:1];
        [heartsArray addObject:heartSprite];
        float deltaX = heartSprite.boundingBox.size.width;
        startHeartX+=deltaX;
    }
    //
}
//
-(void) updateVitaminsCount:(int)newVitaminsCount
{
    NSString *vitaminsStr = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_VITAMINS];
    vitaminsStr = [vitaminsStr stringByAppendingString:[NSString stringWithFormat:@": %d",newVitaminsCount]];
    vitaminsLabel.string = vitaminsStr;
}
//
-(void) createSpecialAbilitieBar:(BOOL) createFlag
{
    //
    CCSprite *timerSprite = [CCSprite spriteWithTexture:[[CCTextureCache sharedTextureCache] addImage:@"boss_life_on.png"]];
    //
    abilitieTimer = [CCProgressTimer progressWithSprite:timerSprite];
    timerSprite.scale = aspectRatio;
    //
    float xCoord = //(([[GameModeManager sharedGameModeManager] currentGameModeType] != kCompaignMode) ?
    [CCDirector sharedDirector].winSize.width / 2.0f;
                    //440.0f*aspectRatio);
    //
    [abilitieTimer setPosition:ccp(xCoord,[CCDirector sharedDirector].winSize.height - 40.0f*aspectRatio)];
    [abilitieTimer setType:kCCProgressTimerTypeBar];
    abilitieTimer.scale = 0.4f*aspectRatio;
    abilitieTimer.midpoint = ccp(-1.0f,0.0f);
    //
    [self addChild:abilitieTimer];
    [abilitieTimer setPercentage:100];
    [abilitieTimer setBarChangeRate:ccp(1,0)];
    //abilitieTimer.opacity = 0.0f;
    //
}
//
-(void) gameHasWon
{
    levelHasWonOrNotLabel.string = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_FINISHED_GAME];
}
//
-(void) updateSpecialAbiliteProgressTimer:(BOOL) updateFlag
{
    if(updateFlag)
    {
        abilitieTimer.opacity = 255;
        float percentage = abilitieTimer.percentage;
        percentage-=(SPECIAL_ABILITE_PROGRESS_STEP);
        abilitieTimer.percentage = percentage;
        //
        if(startSlowMotionFade == NO) {
            [slowMoSprite runAction:[CCFadeTo actionWithDuration:0.2f opacity:50.0f]];
            startSlowMotionFade = YES;
        }
    }
    else
    {
        [slowMoSprite runAction:[CCFadeTo actionWithDuration:0.2f opacity:0.0f]];
        startSlowMotionFade = NO;
        abilitieTimer.opacity = 255;
        [self reFillSpecialAbilitieBar];
    }
}
//
-(void) reFillSpecialAbilitieBar
{
    refillTimer = [NSTimer scheduledTimerWithTimeInterval:SPECIAL_ABILITIE_TIMER_STEP
                                                   target:self
                                                 selector:@selector(reFillSpecialAbilitieBarTimer:)
                                                 userInfo:nil
                                                  repeats:YES];
}
//
-(void) reFillSpecialAbilitieBarTimer:(NSTimer*)timer
{
    if([[GameManager sharedGameManager] gameIsPaused] == NO)
    {
        //
        float percentage = abilitieTimer.percentage;
        percentage+=(SPECIAL_ABILITIE_TIMER_STEP*20);
        abilitieTimer.percentage = percentage;
        //
        if(percentage>=100)
        {
            abilitieTimer.percentage = 100;
            if(refillTimer)
            {
                [refillTimer invalidate];
                refillTimer = nil;
                //
                [[GameManager sharedGameManager] reFillSpecialAbilitieBar];
                //
            }
        }
    }
}
//
-(void) updateHeartCount:(BOOL) increase
{
    //
    CCSprite *lastHeartSprite = (CCSprite*)[heartsArray lastObject];
    //
    if(increase == YES)
    {
        if([heartsArray count] == 3)
            return;
        //
        float startX = lastHeartSprite.boundingBox.size.width + lastHeartSprite.position.x;
        float startY = lastHeartSprite.position.y;
        //
        CCSprite *heartSprite = [CCSprite spriteWithTexture:[[CCTextureCache sharedTextureCache] addImage:@"heart.png"]];
        heartSprite.scale = 0.5f*aspectRatio;
        heartSprite.position = ccp(startX,startY);
        [self addChild:heartSprite z:1];
        [heartsArray addObject:heartSprite];
    }
    //
    else
    {
        [lastHeartSprite removeFromParentAndCleanup:YES];
        [heartsArray removeLastObject];
    }
    //
}
//
-(void) gameOver
{
    //
    levelHasWonOrNotLabel.string = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_GAME_OVER];
    [NSTimer scheduledTimerWithTimeInterval:2.0f
                                     target:self
                                   selector:@selector(showGameOverInTime:)
                                   userInfo:nil
                                    repeats:NO];
}
//
-(void) showGameOverInTime:(NSTimer*)timer
{
    //
	[CCMenuItemFont setFontSize:20];
	[CCMenuItemFont setFontName:@"MarkerFelt-Thin"];
	//
    CCMenuItemFont *restart = [CCMenuItemFont itemWithString:[[LocalizationManager sharedLocalizationManager] getValueWithKey:_RESTART]
                                                      target:self selector:@selector(restartGame:)];
    
    CCMenuItemFont *levelSelect = nil;
    
    if([[GameModeManager sharedGameModeManager] currentGameModeType] == kCompaignMode)
    {
        levelSelect = [CCMenuItemFont itemWithString:[[LocalizationManager sharedLocalizationManager]
                                                      getValueWithKey:_CHOOSE_LEVEL]
                                              target:self selector:@selector(levelSelect:)];
    }
    //
    CCMenuItemFont *quit = [CCMenuItemFont itemWithString:[[LocalizationManager sharedLocalizationManager] getValueWithKey:_GOTO_MENU]
                                                   target:self selector:@selector(quit:)];
    //
    if([[GameModeManager sharedGameModeManager] currentGameModeType] == kCompaignMode)
    {
        menu = [CCMenu menuWithItems:restart,levelSelect, quit, nil];
    }
    else menu = [CCMenu menuWithItems:restart, quit, nil];
    //
	[menu alignItemsHorizontally];
	[self addChild:menu];
	//
    [restart setPosition:ccp(0.0f,0.0f)];
    if([[GameModeManager sharedGameModeManager] currentGameModeType] == kCompaignMode)
    {
        [levelSelect setPosition:ccp(0.0f,-40.0f)];
        [quit setPosition:ccp(0.0f,-80.0f)];
    }
    else [quit setPosition:ccp(0.0f,-40.0f)];
}
//
-(void) updateScoreCounterWithValue:(int)value
{
    score+=value;
    NSString *scoreStr = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_SCORE];
    scoreStr = [scoreStr stringByAppendingString:[NSString stringWithFormat:@": %d",score]];
    [scoreCounter setString:[NSString stringWithFormat:scoreStr,score]];
}
//
-(void) prepareGameForStart
{
    //
    if(refillTimer)
    {
        [refillTimer invalidate];
        refillTimer = nil;
    }
    //
    [abilitieTimer setPercentage:100];
    //
    int currentLevelNum = [[[LevelManager sharedLevelManager] currentLevel] number];
    //
    if([[[LevelManager sharedLevelManager] currentLevel] number] % BOSS_LEVEL_CREATE_CONST == 0 &&
       [[[LevelManager sharedLevelManager] currentLevel] number] > 0)
    {
        scoreCounter.opacity = 0;
        showTimer.opacity = 0;
        abilitieTimer.opacity = 0;
        levelHasWonOrNotLabel.string = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_GET_READY_FOR_BOSS];
        slowMoSprite.opacity = 0;
    }
    else {
        scoreCounter.opacity = 255;
        showTimer.opacity = ([[GameModeManager sharedGameModeManager] currentGameModeType] == kCompaignMode ||
                             [[GameModeManager sharedGameModeManager] currentGameModeType] == kPeaceWalkerMode) ? 255 : 0;
        abilitieTimer.opacity = 255;
        slowMoSprite.opacity = 0;
        //
        NSString *levelOverLayerString = @"";
        //NSLog(@"%@",levelOverLayerString);
        if([[GameModeManager sharedGameModeManager] currentGameModeType] == kCompaignMode)
        {
            int levelNumber = [[LevelManager sharedLevelManager] levelCounter];
            levelNumber = (levelNumber == 1) ? levelNumber + 1 : levelNumber;
            NSString *levelNumberString = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_LEVEL];
            levelNumberString = [levelNumberString stringByAppendingString:[NSString stringWithFormat:@"%d",(levelNumber - 1)]];
            levelOverLayerString = levelNumberString;
        }
        //
        // 
        if([[GameModeManager sharedGameModeManager] currentGameModeType] == kPeaceWalkerMode)
        {
            showTimer.string = @"0 : 10";
        }
        //
        levelHasWonOrNotLabel.string = levelOverLayerString;
    }
        
    //
    [NSTimer scheduledTimerWithTimeInterval:2.0f
                                     target:self
                                   selector:@selector(showGetReadyLabel:)
                                   userInfo:nil
                                    repeats:NO];
}
//
-(void) startLevel:(NSTimer*)timer
{
    [[GameManager sharedGameManager] setGameIsPreparing:NO];
    [levelHasWonOrNotLabel setString:@""];
}
//
-(void) showGetReadyLabel:(NSTimer*)timer
{
    //
    NSString *levelOverLayerString = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_GET_READY];
    //
    [NSTimer scheduledTimerWithTimeInterval:1.0f
                                     target:self
                                   selector:@selector(startLevel:)
                                   userInfo:nil
                                    repeats:NO];
    //
    levelHasWonOrNotLabel.string = levelOverLayerString;
}
//
-(void) showHighScore:(BOOL) enabler {}
//
-(void) pause
{
    //
	[CCMenuItemFont setFontSize:20];
	[CCMenuItemFont setFontName:@"MarkerFelt-Thin"];
	//
    CCMenuItemFont *resume = [CCMenuItemFont itemWithString:[[LocalizationManager sharedLocalizationManager] getValueWithKey:_RESUME]
                                                     target:self selector:@selector(resumeGame:)];
    CCMenuItemFont *restart = [CCMenuItemFont itemWithString:[[LocalizationManager sharedLocalizationManager] getValueWithKey:_RESTART]
                                                      target:self selector:@selector(restartGame:)];
    CCMenuItemFont *levelSelect = nil;
    if([[GameModeManager sharedGameModeManager] currentGameModeType] == kCompaignMode)
    {
        levelSelect = [CCMenuItemFont itemWithString:[[LocalizationManager sharedLocalizationManager]
                                     getValueWithKey:_CHOOSE_LEVEL]
                                              target:self selector:@selector(levelSelect:)];
        
    }
    
    CCMenuItemFont *quit = [CCMenuItemFont itemWithString:[[LocalizationManager sharedLocalizationManager] getValueWithKey:_GOTO_MENU]
                                                   target:self selector:@selector(quit:)];
    //
    if([[GameModeManager sharedGameModeManager] currentGameModeType] == kCompaignMode)
    {
        menu = [CCMenu menuWithItems:resume,restart,levelSelect,quit, nil];
    }
    else menu = [CCMenu menuWithItems:resume,restart,quit, nil];
    //
	[menu alignItemsHorizontally];
	[self addChild:menu];
	//
	[resume setPosition:ccp(0.0f,40.0f)];
    [restart setPosition:ccp(0.0f,0.0f)];
    if([[GameModeManager sharedGameModeManager] currentGameModeType] == kCompaignMode)
    {
        [levelSelect setPosition:ccp(0.0f,-40.0f)];
        [quit setPosition:ccp(0.0f,-80.0f)];
    }
    else [quit setPosition:ccp(0.0f,-40.0f)];
}
//
-(void) resumeGame:(id)sender
{
    [menu removeFromParentAndCleanup:YES];
    menu = nil;
    [[GameManager sharedGameManager] resumeGame];
}
//
-(void) removeMenu
{
    [menu removeFromParentAndCleanup:YES];
    menu = nil;
}
//
-(void) updateBonusOnLevelOverLayer:(NSString*)bonusString
{
    //
    showBonusStringLabel.string = bonusString;
    CCDelayTime *delayTime = [CCDelayTime actionWithDuration:2.0f];
    CCCallFunc *bonusDelayHasDone = [CCCallFunc actionWithTarget:self selector:@selector(bonusIsOver)];
    CCSequence *actions = [CCSequence actions:delayTime,bonusDelayHasDone, nil];
    [showBonusStringLabel runAction:actions];
    //
}
//
-(void) bonusIsOver
{
    showBonusStringLabel.string = @"";
}
//
-(void) restartGame:(id)sender
{
    [[GameManager sharedGameManager] simulateGameRestart];
}
//
-(void) levelSelect:(id)sender
{
    [[GameManager sharedGameManager] goToLevelSelect];
}
//
-(void) updateTimeWithString:(NSString*)timeVal
{
    showTimer.string = timeVal;
}
//
-(void) quit:(id)sender
{
    [[GameManager sharedGameManager] goToMenu];
}
//
-(void) gameOverInGameMode
{
    //
    NSString *finalString = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_TIME_OFF];
    finalString = [finalString stringByAppendingString:@"\n"];
    finalString = [finalString stringByAppendingString:scoreCounter.string];
    //
    levelHasWonOrNotLabel.string = finalString;
    [NSTimer scheduledTimerWithTimeInterval:1.0f
                                     target:self
                                   selector:@selector(showGameOverInTime:)
                                   userInfo:nil
                                    repeats:NO];
    //
}
//
-(void) levelHasWon
{
    NSString *scoreStr = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_SCORE];
    scoreStr = [scoreStr stringByAppendingString:[NSString stringWithFormat:@": %d",0]];
    scoreCounter.string = scoreStr;
    //
    levelHasWonOrNotLabel.string = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_YOU_WON];
    [NSTimer scheduledTimerWithTimeInterval:2.0f
                                     target:self
                                   selector:@selector(getReady:)
                                   userInfo:nil
                                    repeats:NO];
}
//
-(void) getReady:(NSTimer*)timer
{
    //
    if([[[LevelManager sharedLevelManager] currentLevel] number] % BOSS_LEVEL_CREATE_CONST == (BOSS_LEVEL_CREATE_CONST - 1))
    {
        scoreCounter.opacity = 0;
        showTimer.opacity = 0;
        abilitieTimer.opacity = 0;
        levelHasWonOrNotLabel.string = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_GET_READY_FOR_BOSS];
        slowMoSprite.opacity = 0;
    }
    else {
        //
        slowMoSprite.opacity = 0;
        //
        NSString *levelOverLayerString = @"";
        //
        if([[GameModeManager sharedGameModeManager] currentGameModeType] == kCompaignMode)
        {
            int levelNumber = [[LevelManager sharedLevelManager] levelCounter];
            NSString *levelNumberString = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_LEVEL];
            levelNumberString = [levelNumberString stringByAppendingString:[NSString stringWithFormat:@"%d",(levelNumber)]];
            levelOverLayerString = levelNumberString;
        }
        //
        levelHasWonOrNotLabel.string = levelOverLayerString;
        scoreCounter.opacity = 255;
        showTimer.opacity = 255;
        abilitieTimer.opacity = 255;
        if(refillTimer)
        {
            [refillTimer invalidate];
            refillTimer = nil;
            abilitieTimer.percentage = 100;
            [[GameManager sharedGameManager] reFillSpecialAbilitieBar];
        }
    }
        
    [NSTimer scheduledTimerWithTimeInterval:2.0f
                                     target:self
                                   selector:@selector(showGetReadyLabel:)
                                   userInfo:nil
                                    repeats:NO];
    //
}
//
-(void) reset
{
    //
    if(refillTimer)
    {
        [refillTimer invalidate];
        refillTimer = nil;
    }
    //
    if(heartsArray.count!=0)
    {
        for(CCSprite *sprite in heartsArray)
        {
            [sprite removeFromParentAndCleanup:YES];
        }
        [heartsArray removeAllObjects];
    }
    //
    showBonusStringLabel.string = @"";
    slowMoSprite.opacity = 0;
    score = 0;
    //
    NSString *scoreStr = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_SCORE];
    scoreStr = [scoreStr stringByAppendingString:[NSString stringWithFormat:@": %d",score]];
    //
    scoreCounter.string = scoreStr;
    showTimer.string = @"1 : 20";
    //
    if([[GameModeManager sharedGameModeManager] currentGameModeType] == kPeaceWalkerMode)
    {
        showTimer.string = @"0 : 10";
    }
    [menu removeFromParentAndCleanup:YES];
    levelHasWonOrNotLabel.string = @"";
    [abilitieTimer setPercentage:100];
    menu = nil;
    [self initHeartz];
}
//
@end








