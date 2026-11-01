//
//  EnemyAgent.h
//  Game
//
//  Created by Giorgi Abelashvili on 10/22/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "Agent.h"
#import "WeaponManager.h"
#import "Bullet.h"
#include <vector>
#import "VitaminManager.h"
//
@class AgentsManager;
//
@interface EnemyAgent : Agent {

	EnemyAgentType enemyAgentType;
    Weapon *weapon;
	vector <Bullet*> enemyBullets;
	float shootDistance;
	float fleeDistance;
	float fleeSpeed;
	float shootRate;
	NSTimer *shootingTimer;
	BOOL shooting;
	CGPoint movementVelocity;
    CCSprite *glowSprite;
    CGPoint randomMovePoint;
    int hitCount;
    int tag;
    float lerpParameter;
    int killScore;
    float opacityDecreaserStep;
    int vitaminsCount;
}
//
@property (nonatomic,assign) int tag;
@property (nonatomic,assign) float shootDistance;
@property (nonatomic,assign) float fleeDistance;
@property (nonatomic,assign) float fleeSpeed;
@property (nonatomic,assign) float shootRate;
@property (nonatomic,retain) Weapon *weapon;
@property (nonatomic,assign) EnemyAgentType enemyAgentType;
@property (nonatomic,assign) CGPoint movementVelocity;
@property (nonatomic,retain) CCSprite *glowSprite;
@property (nonatomic,assign) CGPoint randomMovePoint;
@property (nonatomic,assign) int hitCount;
@property (nonatomic,assign) int killScore;
//
-(void) startShoot;
//
-(void) shooting:(NSTimer*)timer;
//
-(void) endShoot;
//
-(id) createAtPosition:(CGPoint)position;
//
-(void) moveToPosition:(CGPoint) position;
//
-(void) updateEnemyBullets;
//
-(void) addBullet:(Bullet*)bullet;
//
-(void) creatingAnimationHasEnd:(id)sender;
//
-(void) hit;
//
-(void) generateNextRandomPoint;
//
-(void) refreshEnemy;
//
-(void) bigExploded;
//
@end
