//
//  MainGameLayer.h
//  Tanks
//
//  Created by Giorgi Abelashvili on 11/17/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "cocos2d.h"
#import "GameManager.h"
//
@interface MainGameLayer : CCLayer {
	
    GameManager *gameManager;
    id levelOverLayerDelegate;
    float deltaY;
}
//
@property (nonatomic,retain) id levelOverLayerDelegate;
//
+(MainGameLayer*)sharedMainGameLayer;
-(void) initLevelOverLayer;
-(void) showPauseMenu:(BOOL)enable;
-(void)showDistanceTimer:(BOOL)enable;
-(void) tick:(ccTime)dt;
-(void) pauseGame;
-(void) resumeGame;
-(void) showHighScore:(BOOL) enabler;
-(void) updateScoreWithValue:(int)value;
-(void) updateTimeWithString:(NSString*)timeVal;
-(void) startGame;
-(void) quitGame;
-(void) resetLevelOverLayer;
-(void) prepareGameForStart;
-(void) levelHasWon;
-(void) updateHeartCount:(BOOL) increase;
-(void) showGameOver;
-(void) createBossLife:(BOOL) createFlag;
-(void) updateSpecialAbiliteProgressTimer:(BOOL) updateFlag;
-(void) reFillSpecialAbilitieBar;
-(void) updateBonusOnLevelOverLayer:(NSString*)bonusString;
-(void) gameHasWon;
-(void) updateVitaminsCount:(int)newVitaminsCount;
-(void) gameOverInGameMode;
@end
