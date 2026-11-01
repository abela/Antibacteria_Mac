//
//  Serpentus.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/6/13.
//
//

#import "Serpentus.h"
#import "SerpentusBody.h"
#import "Utils.h"
#import "SpawnManager.h"
#import "AiInput.h"
//
#define SERPENTUS_BODIES_COUNT                6
#define SERPENTUS_BODY_DELTA                  40.0f
//
@implementation Serpentus
//
-(id) createAtPosition:(CGPoint)position
{
    if((self = [super init]))
    {
        //
        agentStateType = kStateWaiting;
        enemyAgentType = kSerpentus;
        speed= 10.0f;
        float deltaX = 0.0f;
        killScore = 400;
        lerpParameter = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? 0.95f : 0.87f;
        //
        for (int32 i = 0; i < SERPENTUS_BODIES_COUNT; ++i)
        {
            //
            SerpentusBody *serpentusBody = [[SerpentusBody alloc] createAtPosition:ccp(position.x+deltaX,position.y)];
            //
            serpentusBody.tag = i;
            serpentusBody.sprite.opacity = 0;
            //
            bodies.push_back(serpentusBody);
            deltaX += SERPENTUS_BODY_DELTA;
            //
            CCFadeIn *fadeIn = [CCFadeIn actionWithDuration:0.1f];
            CCCallFunc *fadeInHasDone = [CCCallFunc actionWithTarget:self selector:@selector(creatingAnimationHasEnd:)];
            [serpentusBody.sprite runAction:[CCSequence actions:fadeIn,fadeInHasDone, nil]];
            //
            if(i == 0)
            {
                [serpentusBody.sprite setDisplayFrame:[[CCSpriteFrameCache sharedSpriteFrameCache] spriteFrameByName:@"Enemy1.png"]];
                [serpentusBody.sprite setColor:ccc3(255,255,255)];
            }
        }
        //
        [self generateNextRandomPoint];
        //
        return self;
    }
    //
    return nil;
}
//
//
-(void) refreshEnemy
{
    lerpParameter = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? 0.95f : 0.87f;
}
//
//
-(void) update
{
    [super update];
}
//
//
-(void) generateNextRandomPoint
{
    int randX = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] upperLeft].x
                                    to:[[CoreSettings sharedCoreSettings] upperRight].x];
    //
    int randY = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] bottomRight].y
                                    to:[[CoreSettings sharedCoreSettings] upperRight].y];
    randomMovePoint = (arc4random_uniform(2) == 0) ? [[SpawnManager sharedSpawnManager] getRandomPointAroundPoint:ccp(randX,randY)] :
    [[AiInput sharedAiInput] mainCharacterPosition];
}
//
-(void) creatingAnimationHasEnd:(id)sender
{
    [self generateNextRandomPoint];
    agentStateType = kStateRandomMovement;
    sprite.opacity = 255;
    bodies[0].body->SetActive(true);
}
//
-(void) randomfly
{
    CGPoint characterPosition = randomMovePoint;
	CGPoint currentAgentPosition = bodies[0].sprite.position;
	//
	CGPoint vector = [[Utils sharedUtils] vectorBetweenTwoPoint:currentAgentPosition :characterPosition];
    //
    vector = ccpNormalize(vector);
    faceDirection = vector;
    float rotAlfa = acosf((vector.x * 0) + (vector.y * 1));
    rotAlfa = CC_RADIANS_TO_DEGREES(rotAlfa);
    //NSLog(@"%f",rotAlfa);
    rotAlfa = (characterPosition.x - currentAgentPosition.x < 0) ? -rotAlfa : rotAlfa;
    bodies[0].sprite.rotation = rotAlfa;
    vector = ccpMult(vector, speed);
    movementVelocity = vector;
    [self moveToPosition:vector];
    //
    [self updateBodies];
    //
}
//
-(void) moveToPosition:(CGPoint) position
{
	bodies[0].body->SetLinearVelocity(b2Vec2(position.x,position.y));
	b2Vec2 currentPos = bodies[0].body->GetPosition();
	bodies[0].glowSprite.position = bodies[0].sprite.position = ccp(currentPos.x*ASPECT_RATIO,currentPos.y*ASPECT_RATIO);
    //
    if(ccpDistance(bodies[0].sprite.position, randomMovePoint) < 10.0f)
        [self generateNextRandomPoint];
}
//
-(void) updateBodies
{
    for(int i = 0;i<(int)bodies.size()-1;i++)
    {
        //
        if(bodies[i].flag == kDealloc)
        {
            if (bodies.size() == 2) {
                flag = kDealloc;
                return;
           }
            [bodies[i] releaseAgent];
            [bodies[i] release];
            bodies.erase(bodies.begin() + i);
            bodies[i].tag = 0;
            [bodies[i].sprite setDisplayFrame:[[CCSpriteFrameCache sharedSpriteFrameCache] spriteFrameByName:@"Enemy1.png"]];
            [bodies[i].sprite setColor:ccc3(255,255,255)];
            return;
        }
        //
        else
        {
            //
            CGPoint newLerpPos = ccpLerp(bodies[i].sprite.position, bodies[i+1].sprite.position, lerpParameter);
            //
            CGPoint pos1 = bodies[i].sprite.position;
            CGPoint pos2 = bodies[i+1].sprite.position;
            //
            CGPoint vector = [[Utils sharedUtils] vectorBetweenTwoPoint:pos2 :pos1];
            //
            vector = ccpNormalize(vector);
            faceDirection = vector;
            float rotAlfa = acosf((vector.x * 0) + (vector.y * 1));
            rotAlfa = CC_RADIANS_TO_DEGREES(rotAlfa);
            rotAlfa = (pos1.x - pos2.x < 0) ? -rotAlfa : rotAlfa;
            //
            bodies[i+1].glowSprite.position = bodies[i+1].sprite.position = newLerpPos;
            bodies[i+1].sprite.rotation = rotAlfa;
            newLerpPos.x /= ASPECT_RATIO;
            newLerpPos.y /= ASPECT_RATIO;
            bodies[i+1].body->SetTransform(b2Vec2(newLerpPos.x,newLerpPos.y), 0);
            //
            
        }
    }
}
//
-(void) releaseAgent
{
    for(int i = 0;i<(int)bodies.size();i++)
    {
        [bodies[i] releaseAgent];
        [bodies[i] release];
    }
    bodies.clear();
}
//
@end
