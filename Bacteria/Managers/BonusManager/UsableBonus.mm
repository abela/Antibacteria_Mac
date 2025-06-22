//
//  UsableBonus.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/12/13.
//
//

#import "UsableBonus.h"
#import "Utils.h"
#import "GameResourceManager.h"
#import "SpawnManager.h"
#import "AiInput.h"
#import "GameModeManager.h"
//
ccColor3B colors[7] = {
    {255,0,0},  
    {0,255,0},
    {0,0,255},
    {255,255,0},
    {255,0,255},
    {0,255,255},
    {128,128,255}
};
//
#define FADE_OUT_INTERVAL       15.0f
//
@interface UsableBonus (PrivateMethods)
-(void) goToFade;
-(void) fadeHasDone;
-(void) bonusTick;
@end
//
@implementation UsableBonus
//
//
-(id) initWithType:(BonusObjectType)bonusType andPosition:(CGPoint)position
{
	//
	if((self = [super initWithType:bonusType andPosition:position]))
    {
		type = kBonusObject;
        flag = kDefault;
        bonusObjectType = bonusType;
        sprite = [CCSprite spriteWithFile:@"bonus.png"];
        sprite.position = position;
        agentStateType = kStateRandomMovement;
        width = 1.3;
		height = 1.3f;
        speed = 4.0f;
        //
        for (int i =0; i<7; i++) {
            int currentBonus = (int) bonusType;
            sprite.color = colors[currentBonus];
        }
		//
		position.x /= ASPECT_RATIO;
		position.y /= ASPECT_RATIO;
		//
        [self createNewBodyAt:position andDimension:ccp(width,height)];
        //
        [self generateNextRandomPoint];
        body->GetFixtureList()->SetSensor(true);
        //
        [self goToFade];
        //
		return self;
	}
	//
	return nil;
}
//
-(void) goToFade
{
    CCFadeOut  *fadeOutAction = [CCFadeOut actionWithDuration:FADE_OUT_INTERVAL];
    CCCallFunc *fadeOutHasDone = [CCCallFunc actionWithTarget:self selector:@selector(fadeHasDone)];
    CCSequence *actions = [CCSequence actions:fadeOutAction,fadeOutHasDone, nil];
    [sprite runAction:actions];
}
//
-(void) fadeHasDone
{
    [sprite stopAllActions];
    flag = kDealloc;
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
-(void) update
{
    if (sprite.opacity < 70) {
        flag = kDealloc;
        return;
    }
	[self randomfly];
    if([[GameModeManager sharedGameModeManager] currentGameModeType] == kCompaignMode)
        [self bonusTick];
}
//
-(void) bonusTick
{
    bonusChangeTick+=GAME_NEXT_SIMULATION_STEP_INTERVAL;
    if(bonusChangeTick >= 1.0f)
    {
        bonusChangeTick = 0.0f;
        int currentBonusType = (int) bonusObjectType;
        currentBonusType = (currentBonusType == 7) ? 0 : (currentBonusType + 1);
        sprite.color = colors[currentBonusType];
        bonusObjectType = (BonusObjectType)currentBonusType;
        //
    }
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
    [self moveToPosition:vector];
}
//
-(void) moveToPosition:(CGPoint)position
{
    body->SetLinearVelocity(b2Vec2(position.x,position.y));
	b2Vec2 currentPos = body->GetPosition();
	sprite.position = ccp(currentPos.x*ASPECT_RATIO,currentPos.y*ASPECT_RATIO);
    //
    if(ccpDistance(sprite.position, randomMovePoint) < 10.0f)
        [self generateNextRandomPoint];
}
//
-(void) releaseAgent
{
    bonusChangeTick = 2.0f;
    [sprite stopAllActions];
	[super releaseAgent];
}
//

@end
