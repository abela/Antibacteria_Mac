//
//  MainGameOverLayer.h
//  Tanks
//
//  Created by Giorgi Abelashvili on 11/17/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "cocos2d.h"
//#import "MainGameLayer.h"

@class MainGameLayer;

@interface LevelOverLayer : CCLayer {
    //
	CCLabelTTF *scoreCounter;
    CCLabelTTF *showTimer;
    CCLabelTTF *levelHasWonOrNotLabel;
    CCLabelTTF *showBonusStringLabel;
    CCLabelTTF *vitaminsLabel;
    //
    CCSprite *slowMoSprite;
    //
    MainGameLayer *gameLayer;
    int score;
    CCMenu *menu;
    
    NSMutableArray *heartsArray;
    CCProgressTimer *abilitieTimer;
    BOOL bossLevel;
    NSTimer *refillTimer;
    
    BOOL startSlowMotionFade;
    float aspectRatio;
    
	//
}

@property (nonatomic, retain) id mainGameLayerdelegate;
//
- (id)  initWithGameLayer:(MainGameLayer*)g;
-(void) initLevelOverLayer;
-(void) updateScoreCounterWithValue:(int)value;
-(void) showHighScore:(BOOL) enabler;
-(void) pause;
-(void) gameOver;
-(void) showGameOverInTime:(NSTimer*)timer;

-(void) updateTimeWithString:(NSString*)timeVal;
-(void) getReady:(NSTimer*)timer;
-(void) reset;
-(void) updateHeartCount:(BOOL) increase;
-(void) removeMenu;
-(void) prepareGameForStart;
-(void) levelHasWon;
-(void) gameHasWon;
-(void) createSpecialAbilitieBar:(BOOL) createFlag;
-(void) updateSpecialAbiliteProgressTimer:(BOOL) updateFlag;
-(void) reFillSpecialAbilitieBar;
-(void) updateBonusOnLevelOverLayer:(NSString*)bonusString;
-(void) updateVitaminsCount:(int)newVitaminsCount;
-(void) gameOverInGameMode;
@end
