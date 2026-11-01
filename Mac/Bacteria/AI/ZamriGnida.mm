//
//  ZamriGnida.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/10/13.
//
//
//
#import "ZamriGnida.h"
#import "GameResourceManager.h"
#import "AiInput.h"
#import "Utils.h"
//
@implementation ZamriGnida
//
-(id) createAtPosition:(CGPoint)position
{
	//
    flag = kDefault;
    agentStateType = kStateWaiting;
    type = kEnemy;
    enemyAgentType = kZamriGnida;
    speed = 6.0f;
    shootRate = 0.5f;
    shootDistance = 100;
    faceDirection = ccp(0.0f,1.0f);
    hitCount = 1;
    fleeDistance = 300;
    killScore = 250;
    //
    radius = [Utils getRandomNumber:10 to:20];
    alphaDelta = [Utils getRandomNumber:1 to:3];
    //
    width = 2.0f;
    height = 2.0f;
    vitaminsCount = 6;
    //
    perpendicularRandomer = arc4random_uniform(2);
    //
    sprite = [CCSprite spriteWithSpriteFrameName:@"Enemy3.png"];
    glowSprite = [CCSprite spriteWithSpriteFrameName:@"Shine2.png"];
    //
    glowSprite.position = sprite.position = position;
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
    return self;
}
//
-(void) creatingAnimationHasEnd:(id)sender
{
    [super creatingAnimationHasEnd:sender];
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
	CGPoint characterPosition = [[AiInput sharedAiInput] mainCharacterPosition];
	CGPoint currentAgentPosition = sprite.position;
	//
	CGPoint vector = [[Utils sharedUtils] vectorBetweenTwoPoint:currentAgentPosition :characterPosition];
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
    float indexer = (sprite.rotation > 0) ? -1 : 1;
    if(fabs(sprite.rotation ) >= fabs((180 - indexer*[[AiInput sharedAiInput] mainCharacterRotation]) - EVADE_DELTA_ALPHA) &&
       fabs(sprite.rotation ) <= fabs((180 - indexer*[[AiInput sharedAiInput] mainCharacterRotation]) + EVADE_DELTA_ALPHA))
    {
        if([[AiInput sharedAiInput] characterIsshooting])
        {
            [sprite setColor:ccc3(255,255,255)];
            [self fastEvade];
        }
    }
    //
    else [sprite setColor:ccc3(0,0,255)];
    //
    [self moveToPosition:movementVelocity];
    //
    [self moveOnCircle];
    //
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
        radius = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? [Utils getRandomNumber:12 to:20]:[Utils getRandomNumber:7 to:16];
        alphaDelta = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? [Utils getRandomNumber:2 to:4] : [Utils getRandomNumber:1 to:3];
        spiralAlpha = 0;
    }
}
//
-(void) fastEvade
{
    CGPoint newVec = (perpendicularRandomer == 0) ? ccpRPerp(movementVelocity) : ccpPerp(movementVelocity);
    newVec = ccpMult(newVec, [[GameManager sharedGameManager] bulletinTimeIsOn] ? 1.3*speed : speed);
    newVec = ccp(newVec.x,-newVec.y);
    movementVelocity = ccpAdd(movementVelocity, newVec);
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
