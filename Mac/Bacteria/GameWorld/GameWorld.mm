//
//  GameWorld.m
//  Bacteria
//
//  Created by Giorgi Abelashvili on 2/11/13.
//  Copyright (c) 2013 __MyCompanyName__. All rights reserved.
//
//
#import "GameWorld.h"
#import "MainGameLayer.h"
#import "GameResourceManager.h"
#import "RedNoise.h"
#import "Bounder.h"
#import "AgentsManager.h"
#import "Utils.h"
#import "LevelManager.h"
#import "Level.h"
#import "GameModeManager.h"
#import "BonusManager.h"
//
#define ENEMY_CREATE_TIMER_DELTA                   10
#define BOUNDER_DELTA                              30
//
@interface GameWorld (PrivateMethods)
-(void) initMiscelaniousGameWorld;
-(void) initGameWorldParticles;
-(void) initWorldBoundaries;
-(void) initBackground;
-(void) initGameWorld;
@end
//
@implementation GameWorld
//
@synthesize gameLayer;
//
-(id) initWithGameLayer:(MainGameLayer*)g
{
    if((self = [super init]))
    {
        // save gamelayer
        gameLayer = g;
        [[BonusManager sharedBonusManager] setGameLayer:g];
        // init gameworld
        [(CCNode*)[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] setParent:nil];
        [(CCNode*)[[GameResourceManager sharedGameResourceManager] sharedVitaminsSpriteSheet] setParent:nil];
        [(CCNode*)[[GameResourceManager sharedGameResourceManager] sharedEnemyArrowDirectioSpriteSheet] setParent:nil];
        //
        [gameLayer addChild:(CCNode*)[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] z:1 tag:100];
        [gameLayer addChild:(CCNode*)[[GameResourceManager sharedGameResourceManager] sharedVitaminsSpriteSheet] z:1 tag:100];
        [gameLayer addChild:(CCNode*)[[GameResourceManager sharedGameResourceManager] sharedEnemyArrowDirectioSpriteSheet] z:1 tag:100];
        //
        return self;
    }
    return nil;
}
//
-(void) refreshGameWorld
{
    [[GameModeManager sharedGameModeManager] refreshGameModeManager];
}
//
-(void) initBackground
{
    //
    backgroundSprite = [CCSprite spriteWithFile:@"bg.jpg"];
    //
    float bcPosX = [CCDirector sharedDirector].winSize.width / 2.0f;
    float bcPosY = [CCDirector sharedDirector].winSize.height / 2.0f;
    backgroundSprite.position = ccp(bcPosX, bcPosY);
    //backgroundSprite.scale = 0.1f;
    [gameLayer addChild:backgroundSprite z:-1 tag:0];
    //
    CGPoint upperLeftPoint = ccp(bcPosX - backgroundSprite.boundingBox.size.width / 2.0f + ENEMY_CREATE_BORDER_DELTA,
                                 bcPosY + backgroundSprite.boundingBox.size.height / 2.0f - ENEMY_CREATE_BORDER_DELTA);
    //
    CGPoint upperRightPoint = ccp(bcPosX + backgroundSprite.boundingBox.size.width / 2.0f - ENEMY_CREATE_BORDER_DELTA,
                                  bcPosY + backgroundSprite.boundingBox.size.height / 2.0f - ENEMY_CREATE_BORDER_DELTA);
    //
    CGPoint bottomLeftPoint = ccp(bcPosX - backgroundSprite.boundingBox.size.width / 2.0f + ENEMY_CREATE_BORDER_DELTA,
                                  bcPosY - backgroundSprite.boundingBox.size.height / 2.0f + ENEMY_CREATE_BORDER_DELTA);
    //
    CGPoint bottomRightPoint = ccp(bcPosX + backgroundSprite.boundingBox.size.width / 2.0f - ENEMY_CREATE_BORDER_DELTA,
                                   bcPosY - backgroundSprite.boundingBox.size.height / 2.0f + ENEMY_CREATE_BORDER_DELTA);
    //
    [[CoreSettings sharedCoreSettings] setUpperLeft:upperLeftPoint];
    [[CoreSettings sharedCoreSettings] setUpperRight:upperRightPoint];
    [[CoreSettings sharedCoreSettings] setBottomLeft:bottomLeftPoint];
    [[CoreSettings sharedCoreSettings] setBottomRight:bottomRightPoint];
    //
}
//
-(void) initGameWorld
{
    [AgentsManager sharedAgentsManager];
    [self initBackground];
    [self initWorldBoundaries];
}
//
-(void) initWorldBoundaries
{
    //
    float bounderDimension = 100.0f;
    //
    CGPoint leftBounderPosition = ccp([[CoreSettings sharedCoreSettings] upperLeft].x - bounderDimension,
                                      ([[CoreSettings sharedCoreSettings] bottomLeft].y +
                                       [[CoreSettings sharedCoreSettings] upperLeft].y) / 2.0f);
    //
    CGPoint leftBounderDimension = ccp(bounderDimension,[[CoreSettings sharedCoreSettings] upperLeft].y -
                                                        [[CoreSettings sharedCoreSettings] bottomLeft].y);
    //
    CGPoint upperBounderPosition = ccp(([[CoreSettings sharedCoreSettings] upperLeft].x +
                                       [[CoreSettings sharedCoreSettings] upperRight].x) / 2.0f,
                                       [[CoreSettings sharedCoreSettings] upperRight].y + bounderDimension);
    //
    CGPoint upperBounderDimension = ccp([[CoreSettings sharedCoreSettings] upperRight].x -
                                        [[CoreSettings sharedCoreSettings] upperLeft].x,
                                        bounderDimension);
    //
    CGPoint rightBounderPosition = ccp([[CoreSettings sharedCoreSettings] upperRight].x + bounderDimension,
                                       ([[CoreSettings sharedCoreSettings] bottomRight].y +
                                        [[CoreSettings sharedCoreSettings] upperRight].y) / 2.0f);
    //
    CGPoint rightBounderDimension = ccp(bounderDimension,[[CoreSettings sharedCoreSettings] upperRight].y -
                                                         [[CoreSettings sharedCoreSettings] bottomRight].y);
    //
    CGPoint bottomBounderPosition = ccp(([[CoreSettings sharedCoreSettings] bottomLeft].x +
                                         [[CoreSettings sharedCoreSettings] bottomRight].x) / 2.0f,
                                        [[CoreSettings sharedCoreSettings] bottomRight].y - bounderDimension);
    //
    CGPoint bottmBounderDimension = ccp([[CoreSettings sharedCoreSettings] bottomRight].x -
                                        [[CoreSettings sharedCoreSettings] bottomLeft].x,
                                        bounderDimension);
    //
    leftBounderPosition = ccp(leftBounderPosition.x/ASPECT_RATIO,leftBounderPosition.y/ASPECT_RATIO);
    leftBounderDimension = ccp(leftBounderDimension.x/ASPECT_RATIO,leftBounderDimension.y/ASPECT_RATIO);
    //
    upperBounderPosition = ccp(upperBounderPosition.x/ASPECT_RATIO,upperBounderPosition.y/ASPECT_RATIO);
    upperBounderDimension = ccp(upperBounderDimension.x/ASPECT_RATIO,upperBounderDimension.y/ASPECT_RATIO);
    //
    rightBounderPosition = ccp(rightBounderPosition.x/ASPECT_RATIO,rightBounderPosition.y/ASPECT_RATIO);
    rightBounderDimension = ccp(rightBounderDimension.x/ASPECT_RATIO,rightBounderDimension.y/ASPECT_RATIO);
    //
    bottomBounderPosition = ccp(bottomBounderPosition.x/ASPECT_RATIO,bottomBounderPosition.y/ASPECT_RATIO);
    bottmBounderDimension = ccp(bottmBounderDimension.x/ASPECT_RATIO,bottmBounderDimension.y/ASPECT_RATIO);
    //
    bounders = [[NSMutableArray alloc] init];
    //
    Bounder *leftBounder = [[Bounder alloc] initAtPosition:leftBounderPosition
                                          andWithDimension:leftBounderDimension
                                           withBounderType:kBounder];
    //
    Bounder *rightBounder = [[Bounder alloc] initAtPosition:rightBounderPosition
                                           andWithDimension:rightBounderDimension
                                            withBounderType:kBounder];
    //
    Bounder *upperBounder = [[Bounder alloc] initAtPosition:upperBounderPosition
                                           andWithDimension:upperBounderDimension
                                            withBounderType:kBounder];
    Bounder *bottomBounder = [[Bounder alloc] initAtPosition:bottomBounderPosition
                                            andWithDimension:bottmBounderDimension
                                             withBounderType:kBounder];
    //
    [bounders addObject:leftBounder];
    [bounders addObject:rightBounder];
    [bounders addObject:upperBounder];
    [bounders addObject:bottomBounder];
}
//
-(void) Run
{
    //
    [self initGameWorld];
    //
    [[GameModeManager sharedGameModeManager] Run];
    //
}
//
-(void) simulateGameWorldRestart
{
    [[GameModeManager sharedGameModeManager] simulateGameModeManagerRestart];
}
//
-(void) gameWorldSpecialAction
{
    [[GameModeManager sharedGameModeManager] gameModeSpecialAction];
}
//
-(void) update
{
    [[GameModeManager sharedGameModeManager] update];
}
//
-(void) gameModeActionLoadNextLevel
{
    [[GameModeManager sharedGameModeManager] gameModeActionLoadNextLevel];
}
//
-(void) stopGameWorldAfterCharacterDeath
{
    [[GameModeManager sharedGameModeManager] stopGameModeManagerAfterCharacterDeath];
}
//
-(void) releaseGameWorld
{
    //bc sprite
    [backgroundSprite removeFromParentAndCleanup:YES];
    //
    // bounders
    for(int i =0; i < (int)[bounders count]; i++ )
    {
        [[bounders objectAtIndex:i] resetBounder];
    }
    //
    [bounders removeAllObjects];
    [bounders release];
    bounders = nil;
    //
    // release current game mode
    [[GameModeManager sharedGameModeManager] releaseGameModeManager];
    //
    [[[GameResourceManager sharedGameResourceManager] sharedVitaminsSpriteSheet] removeFromParentAndCleanup:YES];
    [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] removeFromParentAndCleanup:YES];
    [[[GameResourceManager sharedGameResourceManager] sharedEnemyArrowDirectioSpriteSheet] removeFromParentAndCleanup:YES];
    //
}
//
@end
