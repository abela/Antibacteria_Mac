//
//  ZigZagus.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/8/13.
//
//

#import "ZigZagus.h"
#import "GameResourceManager.h"
#import "Utils.h"
#import "AiInput.h"

@implementation ZigZagus
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
		enemyAgentType = kZigzagus;
		speed = 7.0f;
        sineMoveSpeed = 20.0f;
		shootRate = 0.5f;
		shootDistance = 100;
		faceDirection = ccp(0.0f,1.0f);
        hitCount = 1;
        killScore = 160;
		//
		width = 2.0f;
		height = 2.0f;
        //
        alphaConst = 5.0f;
        vitaminsCount = 4;
		//
		sprite = [CCSprite spriteWithSpriteFrameName:@"Enemy3.png"];
        glowSprite = [CCSprite spriteWithSpriteFrameName:@"Shine2.png"];
        //
		nextZigZagPoint = glowSprite.position = sprite.position = position;
		//
		position.x /= ASPECT_RATIO;
		position.y /= ASPECT_RATIO;
		//
        [super createNewBodyAt:position andDimension:ccp(width,height)];
		//
		[sprite setColor:ccc3(0,0,255)];
        sprite.opacity = 0.0f;
        body->SetActive(false);
        //
        [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:glowSprite z:0 tag:2];
        [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:sprite z:1 tag:1];
		//
        CCFadeIn *fadeIn = [CCFadeIn actionWithDuration:0.1f];
        CCCallFunc *fadeInHasDone = [CCCallFunc actionWithTarget:self selector:@selector(creatingAnimationHasEnd:)];
        [sprite runAction:[CCSequence actions:fadeIn,fadeInHasDone, nil]];
        //
        [self generateNextRandomPoint];
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
    agentStateType = kStatePatrol;
}
//
-(void) generateNextRandomPoint
{
    //
    float x = sineMoveSpeed*cosf(CC_DEGREES_TO_RADIANS(sinAlpha + sprite.rotation));
    float y = sineMoveSpeed*cosf(CC_DEGREES_TO_RADIANS(sinAlpha + sprite.rotation));
    movementVelocity = ccpAdd(movementVelocity, ccp(x,y));
    sinAlpha-=alphaConst;
    if(fabs(sinAlpha) > 360)
    {
        alphaConst = [Utils getRandomNumber:3 to:7];
        alphaConst*=-1;
    }
    //
}
//
-(void) update
{
	[super update];
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
    //
    [self generateNextRandomPoint];
    //
    [super moveToPosition:movementVelocity];
    //
   
}
//
-(void) attack
{
	[super attack];
}
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
