//
//  Anakonda.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/13/13.
//
//

#import "Anakonda.h"
#import "SerpentusBody.h"
#import "Utils.h"
#import "SpawnManager.h"
#import "AiInput.h"
#import "Weapon.h"
//
#define SERPENTUS_BODIES_COUNT                  24
#define SERPENTUS_BODY_DELTA                    40.0f
//
@interface Anakonda (PrivateMethods)
-(void) addBody:(NSTimer*)timer;
@end
//
@implementation Anakonda
//
-(id) createAtPosition:(CGPoint)position
{
    if((self = [super init]))
    {
        //
        agentStateType = kStateWaiting;
        enemyAgentType = kAnakonda;
        speed= 17.0f;
        weapon = [[Weapon alloc] init];
        float deltaX = 0.0f;
        lerpParameter = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? 0.95f : 0.87f;
        shootRate = 1.5f;
        //
        float addBodyTimerConst = 3.0f;
        //
        for (int32 i = 0; i < SERPENTUS_BODIES_COUNT; ++i)
        {
            //
            SerpentusBody *serpentusBody = [[SerpentusBody alloc] createAtPosition:ccp(position.x+deltaX,position.y)];
            [serpentusBody body]->GetFixtureList()->SetSensor(true);
            //
            serpentusBody.hitCount = 14;
            serpentusBody.tag = i;
            serpentusBody.sprite.opacity = 0;
            serpentusBody.sprite.scale = 2.0f;
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
            //
        }
        //
        [self generateNextRandomPoint];
        //
        
        //
        switch ([[GameSettingsManager sharedGameSettingsManager] gameDifficulty]) {
            case kHard:
                prevShootRate = shootRate = 1.0f;
                addBodyTimerConst = 2.0f;
                break;
            case kImpossible:
                prevShootRate = shootRate = 0.5f;
                addBodyTimerConst = 1.8f;
                break;
            default:
                break;
        }
        //
        addBodyTimerValue = [NSTimer scheduledTimerWithTimeInterval:addBodyTimerConst
                                                             target:self
                                                           selector:@selector(addBody:)
                                                           userInfo:nil
                                                            repeats:YES];
        //
        [self startShoot];
        //
        return self;
    }
    //
    return nil;
}
//
//
//
-(void) startShoot
{
    shootingTimer = [NSTimer scheduledTimerWithTimeInterval:shootRate
                                                     target:self
                                                   selector:@selector(shooting:)
                                                   userInfo:nil
                                                    repeats:YES];
}
//
-(void) shooting:(NSTimer *)timer
{
    //
    if([[GameManager sharedGameManager] gameIsPaused] == YES)
        return;
    //
    CGPoint characterPosition = [[AiInput sharedAiInput] mainCharacterPosition];
	CGPoint currentAgentPosition = bodies[0].sprite.position;
	CGPoint vector = [[Utils sharedUtils] vectorBetweenTwoPoint:currentAgentPosition :characterPosition];
    vector = ccpNormalize(vector);
    float rotAlfa = acosf((vector.x * 0) + (vector.y * 1));
    rotAlfa = CC_RADIANS_TO_DEGREES(rotAlfa);
    //
    Bullet *bullet = [weapon shootAtPosition:ccp(bodies[0].sprite.position.x,bodies[0].sprite.position.y)
                                 andRotation:rotAlfa
                                andDirection:vector
                               andStartDelta:30];
    //
    bullet.tag = SHOOTING_COROSIA_BULLET_TAG;
    bullet.body->GetFixtureList()->SetSensor(true);
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
//
-(void) refreshEnemy
{
    [self endShoot];
    shootRate = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? shootRate + 1.0f +(1-[Utils timeScale]) : prevShootRate;
    [self startShoot];
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
    //
    int randX = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] upperLeft].x
                                    to:[[CoreSettings sharedCoreSettings] upperRight].x];
    //
    int randY = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] bottomRight].y
                                    to:[[CoreSettings sharedCoreSettings] upperRight].y];
    //
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
                [[GameManager sharedGameManager] levelHasWonAfterBigBoss];
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
-(void) addBody:(NSTimer*)timer
{
    if([[GameManager sharedGameManager] gameIsPaused] == YES)
        return;
    //
    CGPoint bodPyos = [[SpawnManager sharedSpawnManager] getValidOuterSpawn];
    SerpentusBody *serpentusBody = [[SerpentusBody alloc] createAtPosition:bodPyos];
    [serpentusBody body]->GetFixtureList()->SetSensor(true);
    //
    serpentusBody.hitCount = 5;
    serpentusBody.tag = bodies.size()+1;
    serpentusBody.sprite.opacity = 0;
    serpentusBody.sprite.scale = 2.0f;
    //
    bodies.push_back(serpentusBody);
}
//
-(void) releaseAgent
{
    //
    if(hitCount<=0)
    {
        [[VitaminManager sharedVitaminManager] createVitaminAtPosition:sprite.position withScore:10];
    }
    //
    for(int i = 0;i<(int)bodies.size();i++)
    {
        [bodies[i] releaseAgent];
        [bodies[i] release];
    }
    bodies.clear();
    if(addBodyTimerValue!=nil)
    {
        [addBodyTimerValue invalidate];
        addBodyTimerValue = nil;
    }
    //
    if (shootingTimer)
    {
        [shootingTimer invalidate];
        shootingTimer = nil;
    }
    //
    [weapon removeAllBulletsInstantly];
    [weapon release];
    weapon = nil;
    //
}
//
@end
