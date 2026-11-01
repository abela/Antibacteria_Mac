//
//  EnemyAgent.mm
//  Game
//
//  Created by Giorgi Abelashvili on 10/22/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "EnemyAgent.h"
#import "WeaponManager.h"
#import "AiInput.h"
#import "Utils.h"
#import "SpawnManager.h"
#import "Weapon.h"
//
@interface EnemyAgent(EnemyAgentPrivateFunctions)
@end
//
@implementation EnemyAgent

//
@synthesize weapon;
@synthesize shootDistance;
@synthesize fleeDistance;
@synthesize fleeSpeed;
@synthesize hitCount;
//
@synthesize enemyAgentType;
@synthesize shootRate;
@synthesize glowSprite;
//
@synthesize movementVelocity;
@synthesize randomMovePoint;
@synthesize killScore;
//
@synthesize tag;
//
-(id) createAtPosition:(CGPoint)position
{
	//
	return self;
	//
}
//
-(void) addBullet:(Bullet*)bullet
{
    enemyBullets.push_back(bullet);
}
//
-(void) moveToPosition:(CGPoint) position
{
    //position = ccpMult(position, [Utils timeScale]);
	body->SetLinearVelocity(b2Vec2(position.x,position.y));
	b2Vec2 currentPos = body->GetPosition();
	glowSprite.position = sprite.position = ccp(currentPos.x*ASPECT_RATIO,currentPos.y*ASPECT_RATIO);
}
//
-(void) patrol
{
	CGPoint characterPosition = [[AiInput sharedAiInput] mainCharacterPosition];
	CGPoint currentAgentPosition = sprite.position;
	//
	CGPoint vector = [[Utils sharedUtils] vectorBetweenTwoPoint:currentAgentPosition :characterPosition];
    //
    vector = ccpNormalize(vector);
    faceDirection = vector;
    float rotAlfa = acosf((vector.x * 0) + (vector.y * 1));
    rotAlfa = CC_RADIANS_TO_DEGREES(rotAlfa);
    //NSLog(@"%f",rotAlfa);
    rotAlfa = (characterPosition.x - currentAgentPosition.x < 0) ? -rotAlfa : rotAlfa;
    sprite.rotation = rotAlfa;
    vector = ccpMult(vector, speed);
    movementVelocity = vector;
    [self moveToPosition:movementVelocity];
    //
}
//
-(void) update
{
    //
	switch (agentStateType) {
        case kStateWaiting:
            break;
		case kStatePatrol:
			[self patrol];
			break;
			//
		case kStateAttack:
			[self attack];
			break;
			//
		case kStateFlee:
			[self flee];
			break;
        case kStateRandomMovement:
            [self randomfly];
            break;
        case kStateEvade:
            [self evade];
            break;
        case kStateIdle:
            [self idle];
            break;
		default:
			break;
	}
	//
	[self updateEnemyBullets];
	//
}
//
-(void) idle
{
}
//
-(void) fastEvade
{
    
}
//
-(void) randomfly
{
    //
    CGPoint characterPosition = randomMovePoint;
	CGPoint currentAgentPosition = sprite.position;
	//
	CGPoint vector = [[Utils sharedUtils] vectorBetweenTwoPoint:currentAgentPosition :characterPosition];
	///float length = [[Utils sharedUtils] calculateVectorLength:vector];
    vector = ccpNormalize(vector);
    faceDirection = vector;
    float rotAlfa = acosf((vector.x * 0) + (vector.y * 1));
    rotAlfa = CC_RADIANS_TO_DEGREES(rotAlfa);
    //NSLog(@"%f",rotAlfa);
    rotAlfa = (characterPosition.x - currentAgentPosition.x < 0) ? -rotAlfa : rotAlfa;
    //sprite.rotation = rotAlfa;
    vector = ccpMult(vector, speed);
    movementVelocity = vector;
    [self moveToPosition:vector];

}
//
-(void) flee
{
	//
	CGPoint characterPosition = [[AiInput sharedAiInput] mainCharacterPosition];
	CGPoint currentAgentPosition = sprite.position;
	CGPoint vector = [[Utils sharedUtils] vectorBetweenTwoPoint:currentAgentPosition :characterPosition];
	//
	//run away
	if(ccpDistance(characterPosition, currentAgentPosition) < fleeDistance)
	{
		//NSLog(@"run away");
		CGPoint desiredVelocity = ccpNormalize(ccpSub(currentAgentPosition, characterPosition));
		desiredVelocity = ccpMult(desiredVelocity, fleeSpeed);
		b2Vec2 currentVelocity = body->GetLinearVelocity();
		CGPoint finalDesiredVelocity = ccpSub(desiredVelocity, ccp(currentVelocity.x,currentVelocity.y));
		//body->SetLinearVelocity(b2Vec2(finalDesiredVelocity.x,finalDesiredVelocity.y));
		[self moveToPosition:finalDesiredVelocity];
		//
		vector = ccpNormalize(vector);
		faceDirection = vector;
		float rotAlfa = acosf((vector.x * 0) + (vector.y * 1));
		rotAlfa = CC_RADIANS_TO_DEGREES(rotAlfa);
		//NSLog(@"%f",rotAlfa);
		rotAlfa = (characterPosition.x - currentAgentPosition.x < 0) ? -rotAlfa : rotAlfa;
		sprite.rotation = rotAlfa;		
		//
	}
	//
	else {
		agentStateType = kStatePatrol;
		body->SetLinearVelocity(b2Vec2(0.0f,0.0f));
	}

	//
		
	//
}
//
-(void) refreshEnemy
{
}
//
-(void) startShoot
{
	//
	shootingTimer = [NSTimer scheduledTimerWithTimeInterval:shootRate
													 target:self
												   selector:@selector(shooting:)
												   userInfo:nil
													repeats:YES];
	//
}
//
-(void) generateNextRandomPoint
{
    
}
//
-(void) shooting:(NSTimer*)timer
{
    
}
//
-(void) endShoot
{
	if(shootingTimer!=nil)
	{
		[shootingTimer invalidate];
		shootingTimer = nil;
		shooting = NO;
	}
}
//
-(void) attack
{
	CGPoint characterPosition = [[AiInput sharedAiInput] mainCharacterPosition];
	CGPoint currentAgentPosition = sprite.position;
	//
	CGPoint vector = [[Utils sharedUtils] vectorBetweenTwoPoint:currentAgentPosition :characterPosition];
	float length = [[Utils sharedUtils] calculateVectorLength:vector];
	//
	vector = ccpNormalize(vector);
	faceDirection = vector;
	float rotAlfa = acosf((vector.x * 0) + (vector.y * 1));
	rotAlfa = CC_RADIANS_TO_DEGREES(rotAlfa);
	//NSLog(@"%f",rotAlfa);
	rotAlfa = (characterPosition.x - currentAgentPosition.x < 0) ? -rotAlfa : rotAlfa;
	sprite.rotation = rotAlfa;
	//
	//
	if(shooting == NO)
	{
		[self startShoot];
		shooting = YES;
	}
	//
	if(length>=shootDistance)
	{
		agentStateType = kStatePatrol;
		[self endShoot];
	}
	//
}
//
-(void) creatingAnimationHasEnd:(id)sender
{
    sprite.opacity = 255;
    body->SetActive(true);
}
//
-(void) bigExploded
{
    if(ccpDistance([[AiInput sharedAiInput] mainCharacterPosition], sprite.position) <= BIG_EXPLOSION_DISTANCE)
    {
        hitCount-=BIG_EXPLOSION_HIT_CONST ;
        flag = (hitCount <= 0) ? kDealloc : kDefault;
    }
}
//
-(void) updateEnemyBullets
{
    [weapon updateBullets];
}
//
-(void) hit
{
    hitCount -- ;
    flag = (hitCount <= 0) ? kDealloc : kDefault;
}
//
-(void) releaseAgent
{
    //
	[super resetBody];
    [sprite removeFromParentAndCleanup:YES];
    [glowSprite removeFromParentAndCleanup:YES];
	//
}
@end
