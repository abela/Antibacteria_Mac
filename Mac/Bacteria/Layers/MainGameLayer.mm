//
//  MainGameLayer.m
//  Tanks
//
//  Created by Giorgi Abelashvili on 11/17/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

#import "MainGameLayer.h"
#import "Figure.h"
#import "Utils.h"
#import "LevelOverLayer.h"
//
#define PAUSE_BUTTON_RECT           CGRectMake(460.0,300.0,30.0,30.0)
#define PAUSE_BUTTON_RECT_IPAD      CGRectMake(990.0f,730.0f,60.0f,60.0f)
//
#define accelerometerFrequency          60.0f
//
@implementation MainGameLayer
//
//
@synthesize levelOverLayerDelegate;
//
static MainGameLayer *_sharedMainGameLayer;
//
+(MainGameLayer*)sharedMainGameLayer
{
    //
	@synchronized([CCDirector class])
	{
        //
		if (!_sharedMainGameLayer) 
		{
			_sharedMainGameLayer = [[MainGameLayer alloc] init];
		}
		//
		return _sharedMainGameLayer;
	}
    //
}
//
- (id)init
{
    //
	if (self == [super init]) 
	{
        //
		self.isTouchEnabled = YES;
		return self;
		//
    }
    //
	else return nil;
}
//
-(void) initLevelOverLayer
{
    [(LevelOverLayer*)levelOverLayerDelegate initLevelOverLayer];
}
//
-(void) gameOverInGameMode
{
    [levelOverLayerDelegate gameOverInGameMode];
}
//
-(void) showPauseMenu:(BOOL)enable
{
}
//
-(void)showDistanceTimer:(BOOL)enable
{
	
}
//
-(void) updateVitaminsCount:(int)newVitaminsCount
{
    [levelOverLayerDelegate updateVitaminsCount:newVitaminsCount];
}
//
-(void) pauseGame
{
    [self unschedule:@selector(tick:)];
    [(LevelOverLayer*)levelOverLayerDelegate pause];
}
//
-(void) updateTimeWithString:(NSString*)timeVal
{
    [levelOverLayerDelegate updateTimeWithString:timeVal];
}
//
-(void) resumeGame
{
    //
    [levelOverLayerDelegate removeMenu];
    [self unschedule:_cmd];
    [self schedule:@selector(tick:)];
    //
}
//
//
-(void) tick:(ccTime)dt
{
    [gameManager manageLoop:dt];
}
//
//
-(void) showGameOver
{
    [(LevelOverLayer*)levelOverLayerDelegate gameOver];
}
//
//
-(void) levelHasWon
{
    [(LevelOverLayer*)levelOverLayerDelegate levelHasWon];
}
//
//
-(void) updateHeartCount:(BOOL) increase
{
    [(LevelOverLayer*)levelOverLayerDelegate updateHeartCount:increase];
}
//
//
-(void) prepareGameForStart
{
    [(LevelOverLayer*)levelOverLayerDelegate prepareGameForStart];
}
//
//
-(void) updateScoreWithValue:(int)value
{
    [(LevelOverLayer*)levelOverLayerDelegate updateScoreCounterWithValue:value];
}
//
-(void) onEnterTransitionDidFinish
{
	gameManager = [GameManager sharedGameManager];
    gameManager.mainGameLayerDelegate = self;
    [gameManager initGameManager];
    //
}
//
-(void) gameHasWon
{
    [(LevelOverLayer*)levelOverLayerDelegate gameHasWon];
}
//
-(void) startGame
{
    [self initLevelOverLayer];
    //
    [self schedule:@selector(tick:)];
    //
}
//
//
-(void) reFillSpecialAbilitieBar
{
    
}
//
//
-(void) updateBonusOnLevelOverLayer:(NSString*)bonusString
{
    [(LevelOverLayer*)levelOverLayerDelegate updateBonusOnLevelOverLayer:bonusString];
}
//
//
-(void) updateSpecialAbiliteProgressTimer:(BOOL) updateFlag
{
    [levelOverLayerDelegate updateSpecialAbiliteProgressTimer:updateFlag];
}
//
//
-(void) createBossLife:(BOOL) createFlag
{
    
}
//
//
-(void) quitGame
{
    [self unschedule:@selector(tick:)];
    [self resetLevelOverLayer];
    [levelOverLayerDelegate removeFromParentAndCleanup:YES];
}
//
//
-(void) resetLevelOverLayer
{
    [levelOverLayerDelegate reset];
}
//
//
-(void) showHighScore:(BOOL) enabler
{
    [levelOverLayerDelegate showHighScore:enabler];
}
//
-(void) onExit
{
    [[CCTextureCache sharedTextureCache] removeUnusedTextures];
}
//
@end
