//
//  BigHunterus.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/3/13.
//
//

#import "BigHunterus.h"
#import "AiInput.h"
#import "Utils.h"
#import "GameResourceManager.h"
#import "SpawnManager.h"

@implementation BigHunterus
//
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
		enemyAgentType = kBigHunterus;
		speed = 7.0f;
		fleeSpeed = 5.0f;
		shootRate = 0.5f;
		shootDistance = 100;
		fleeDistance = 70.0f;
		faceDirection = ccp(0.0f,1.0f);
        hitCount = 5;
        killScore = 120;
        vitaminsCount = 5;
		//
		width = 7.0f;
		height = 7.0f;
		//
		sprite = [CCSprite spriteWithSpriteFrameName:@"Enemy1.png"];
        glowSprite = [CCSprite spriteWithSpriteFrameName:@"Shine3.png"];
		//
		glowSprite.position = sprite.position = position;
        glowSprite.scale = sprite.scale = 4.0f;
		//
		position.x /= ASPECT_RATIO;
		position.y /= ASPECT_RATIO;
		//
		[super createNewBodyAt:position andDimension:ccp(width,height)];
		//
        sprite.opacity = 0.0f;
        body->SetActive(false);
        //
		[[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:sprite z:1 tag:1];
        [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:glowSprite z:0 tag:1];
		//
        CCFadeIn *fadeIn = [CCFadeIn actionWithDuration:0.1f];
        CCCallFunc *fadeInHasDone = [CCCallFunc actionWithTarget:self selector:@selector(creatingAnimationHasEnd:)];
        [sprite runAction:[CCSequence actions:fadeIn,fadeInHasDone, nil]];
        //
        body->GetFixtureList()->SetSensor(true);
        //
		return self;
	}
	//
	return nil;
}
//
-(void) generateNextRandomPoint
{
    //
    int randX = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] upperLeft].x
                                    to:[[CoreSettings sharedCoreSettings] upperRight].x];
    //
    int randY = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] bottomRight].y
                                    to:[[CoreSettings sharedCoreSettings] upperRight].y];
    //
    randomMovePoint = [[SpawnManager sharedSpawnManager] getRandomPointAroundPoint:ccp(randX,randY)];
}
//
-(void) creatingAnimationHasEnd:(id)sender
{
    [super creatingAnimationHasEnd:sender];
    [self generateNextRandomPoint];
    agentStateType = kStatePatrol;
}
//
-(void) update
{
	[super update];
}
//
-(void) patrol
{
    [super patrol];
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
-(void) moveToPosition:(CGPoint)position
{
    [super moveToPosition:position];
    if(ccpDistance(sprite.position, randomMovePoint) < 10.0f)
        [self generateNextRandomPoint];
}
//
-(void) flee
{
	[super flee];
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
	if(length<=fleeDistance)
	{
		[self endShoot];
		agentStateType = kStateFlee;
	}
}
//
//
-(void) releaseAgent
{
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
@end
