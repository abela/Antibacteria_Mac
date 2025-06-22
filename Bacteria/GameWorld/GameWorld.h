//
//  GameWorld.h
//  Bacteria
//
//  Created by Giorgi Abelashvili on 2/11/13.
//  Copyright (c) 2013 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "cocos2d.h"
//
@class MainGameLayer;
//
@interface GameWorld : NSObject
{
    MainGameLayer *gameLayer;
    @private
    CCSprite *backgroundSprite;
    NSMutableArray *bounders;
    NSTimer *enemyCreateTimer;
    int enemyCreateTimeCounter;
    float enemyCreateVar;
    float prevEnemyCreateVar;
    NSTimer *realTimeTickVar;
    BOOL weHaveBossLevel;
    BOOL isFirstRun;
}
//
@property (nonatomic,readonly) MainGameLayer *gameLayer;
//
-(id) initWithGameLayer:(MainGameLayer*)g;
-(void) Run;
-(void) gameWorldSpecialAction;
-(void) gameModeActionLoadNextLevel;
-(void) refreshGameWorld;
-(void) releaseGameWorld;
-(void) update;
-(void) simulateGameWorldRestart;
-(void) stopGameWorldAfterCharacterDeath;
//
@end
