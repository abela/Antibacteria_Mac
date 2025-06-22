//
//  ChoruxDefender.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/5/13.
//
//

#import "ChoruxDefender.h"
#import "Utils.h"
#import "GameResourceManager.h"
#import "SpawnManager.h"
#import "AiInput.h"
//
@implementation ChoruxDefender
@synthesize pointToDeffend;
@synthesize targetToDeffend;
//
-(id) createAtPosition:(CGPoint)position
{
	//
	if((self = [super createAtPosition:position]))
	{
        //
        flag = kDefault;
		agentStateType = kStateWaiting;
		type = kEnemy;
		enemyAgentType = kChoruxDefender;
		speed = 7.0f;
		shootRate = 0.5f;
		shootDistance = 100;
		faceDirection = ccp(0.0f,1.0f);
        spiralAlpha = 0.0f;
        hitCount = 1;
        vitaminsCount = 2;
        radius = [Utils getRandomNumber:10 to:20];
        //
		return self;
	}
	//
	return nil;
}
//
-(void) creatingAnimationHasEnd:(id)sender
{
    [super creatingAnimationHasEnd:sender];
}
//
-(void) update
{
    [super update];
    if(targetToDeffend.flag == kDying)
        canHit = YES;
}
//
-(void) patrol
{
    //
	CGPoint characterPosition = [[AiInput sharedAiInput] mainCharacterPosition];
	CGPoint currentAgentPosition = sprite.position;
    //
	CGPoint vector = [[Utils sharedUtils] vectorBetweenTwoPoint:currentAgentPosition :characterPosition];
    vector = ccpNormalize(vector);
    //
    faceDirection = vector;
    //
    float rotAlfa = acosf((vector.x * 0) + (vector.y * 1));
    rotAlfa = CC_RADIANS_TO_DEGREES(rotAlfa);
    rotAlfa = (characterPosition.x - currentAgentPosition.x < 0) ? -rotAlfa : rotAlfa;
    //
    sprite.rotation = rotAlfa;
    vector = ccpMult(vector, speed);
    movementVelocity = vector;
    //
    [super moveToPosition:vector];
    //
    [self moveOnCircle];
}
//
-(void) moveOnCircle
{
    float x = radius*cosf(CC_DEGREES_TO_RADIANS(spiralAlpha));
    float y = radius*sinf(CC_DEGREES_TO_RADIANS(spiralAlpha));
    CGPoint newPos = ccpAdd(movementVelocity, ccp(x,y));
    [super moveToPosition:newPos];
    spiralAlpha+=alphaDelta;
    if(spiralAlpha > 360.0f)
    {
        radius = [Utils getRandomNumber:10 to:20];
        alphaDelta = [Utils getRandomNumber:1 to:3];
        spiralAlpha = 0;
    }
}
//
-(void) attack
{
	[super attack];
}
//
-(void) releaseAgent
{
    //
    if(hitCount <= 0)
    {
        for (int i =0; i<vitaminsCount; i++) {
            CGPoint randomPos = [[VitaminManager sharedVitaminManager] getValidBonusSpawnPointAtPoint:sprite.position];
            [[VitaminManager sharedVitaminManager] createVitaminAtPosition:randomPos withScore:1];
        }
    }
    //
	[super releaseAgent];
}
//
-(void) hit
{
    if(canHit == YES)
        [super hit];
}
//
-(void) bigExploded
{
    if(canHit)
        [super bigExploded];
}
///
@end
