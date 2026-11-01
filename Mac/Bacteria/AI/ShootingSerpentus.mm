//
//  SpiralSerpentus.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/8/13.
//
//

#import "ShootingSerpentus.h"
#import "SerpentusBody.h"
#import "Utils.h"
#import "SpawnManager.h"
#import "AiInput.h"
#import "Weapon.h"
//
#define SERPENTUS_BODIES_COUNT                 5
#define SERPENTUS_BODY_DELTA                   40.0f
//

@implementation ShootingSerpentus
//
-(id) createAtPosition:(CGPoint)position
{
    if((self = [super init]))
    {
        //
        agentStateType = kStateWaiting;
        enemyAgentType = kShootingSerpentus;
        speed = 10.0f;
        float deltaX = 0.0f;
        shootRate = 1.5f;
        killScore = 500;
        weapon = [[Weapon alloc] init];
        //
        lerpParameter = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? 0.95f : 0.87f;
        //
        for (int32 i = 0; i < SERPENTUS_BODIES_COUNT; ++i)
        {
            //
            SerpentusBody *serpentusBody = [[SerpentusBody alloc] createAtPosition:ccp(position.x+deltaX,position.y)];
            //
            serpentusBody.tag = i;
            serpentusBody.sprite.opacity = 0;
            serpentusBody.hitCount = 4;
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
        [self startShoot];
        //
        return self;
    }
    //
    return nil;
}
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
-(void) refreshEnemy
{
    [self endShoot];
    shootRate = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? 2.3f+(1-[Utils timeScale]) : 1.5f;
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
    //
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
    if(flag == kDying)
    {
        if([weapon isEmpty])
        {
            flag = kDealloc;
        }
    }
    //
    for(int i = 0;i<(int)bodies.size()-1;i++)
    {
        //
        if(bodies[i].flag == kDealloc)
        {
            if (bodies.size() == 2) {
                flag = kDying;
                for(int j =0;j<(int)bodies.size();j++)
                {
                    bodies[j].glowSprite.opacity = bodies[j].sprite.opacity = 0;
                    bodies[j].body->SetActive(false);
                }
                //
                [self endShoot];
                //
                return;
            }
            //
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
    //
    bodies.clear();
    [self endShoot];
    [weapon removeAllBulletsInstantly];
    [weapon release];
    weapon = nil;
}
//
-(void) bigExploded
{
    if(ccpDistance([[AiInput sharedAiInput] mainCharacterPosition], bodies[0].sprite.position) <= BIG_EXPLOSION_DISTANCE)
    {
        [weapon removeAllBulletsInstantly];
        flag = kDealloc;
    }
}
//
@end
