//
//  Frunctus.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/15/13.
//
//
//
#import "Frunctus.h"
#import "GameResourceManager.h"
#import "AiInput.h"
#import "Utils.h"
#import "Gnida.h"
#import "SpawnManager.h"
#import "Weapon.h"
//
#define LEAVE_EVADE_DISTANCE    40
#define ULTRA_SPEED             80
#define NORMAL_SPEED            6
#define MIN_OPACITY             70
//
@interface Frunctus (PrivateMethods)
-(void) checkEscape;
@end
//
@implementation Frunctus
//
@synthesize timeToEscape;
//
-(id) createAtPosition:(CGPoint)position
{
	//
    position = [[SpawnManager sharedSpawnManager] getValidSpawnPosFromCharacter];
    //
    weapon = [[Weapon alloc] init];
    flag = kDefault;
    agentStateType = kStateWaiting;
    type = kEnemy;
    enemyAgentType = kFrunctus;
    speed = NORMAL_SPEED;
    shootRate = 0.5f;
    shootDistance = 100;
    faceDirection = ccp(0.0f,1.0f);
    hitCount = 1.0f;//150;
    shootRate = 1.5f;
    vitaminsCount = 10;
    //
    switch ([[GameSettingsManager sharedGameSettingsManager] gameDifficulty]) {
        case kHard:
            prevShootRate = shootRate = 0.7f;
            break;
        case kImpossible:
            prevShootRate = shootRate = 0.3f;
            break;
        default:
            break;
    }
    //
    radius = [Utils getRandomNumber:10 to:20];
    alphaDelta = [Utils getRandomNumber:1 to:3];
    //
    width = 2.3f;
    height = 2.3f;
    perpendicularRandomer = arc4random_uniform(2);
    //
    sprite = [CCSprite spriteWithSpriteFrameName:@"Enemy3.png"];
    glowSprite = [CCSprite spriteWithSpriteFrameName:@"Shine2.png"];
    //
    glowSprite.position = sprite.position = position;
    glowSprite.scale = sprite.scale = 2.0f;
    //
    //
    position.x /= ASPECT_RATIO;
    position.y /= ASPECT_RATIO;
    //
    [super createNewBodyAt:position andDimension:ccp(width,height)];
    //
    [sprite setColor:ccc3(0,0,255)];
    body->SetActive(false);
    //
    [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:glowSprite z:0 tag:2];
    [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:sprite z:1 tag:1];
    //
    CCFadeIn *fadeIn = [CCFadeIn actionWithDuration:0.1f];
    CCCallFunc *fadeInHasDone = [CCCallFunc actionWithTarget:self selector:@selector(creatingAnimationHasEnd:)];
    [sprite runAction:[CCSequence actions:fadeIn,fadeInHasDone, nil]];
    //
    [self startShoot];
    //
    opacityDecreaserStep = sprite.opacity / hitCount;
    //
    return self;
}
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
	CGPoint currentAgentPosition = sprite.position;
	CGPoint vector = [[Utils sharedUtils] vectorBetweenTwoPoint:currentAgentPosition :characterPosition];
    vector = ccpNormalize(vector);
    float rotAlfa = acosf((vector.x * 0) + (vector.y * 1));
    rotAlfa = CC_RADIANS_TO_DEGREES(rotAlfa);
    //
    Bullet *bullet = [weapon shootAtPosition:ccp(sprite.position.x,sprite.position.y)
                                 andRotation:sprite.rotation
                                andDirection:vector
                               andStartDelta:50];
    //
    bullet.tag = FRUNCTUS_BULLET_TAG;
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
    shootRate = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? shootRate + 1.0f+(1-[Utils timeScale]) : prevShootRate;
    [self startShoot];
    lerpParameter = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? 0.95f : 0.87f;
}
//
//
-(void) creatingAnimationHasEnd:(id)sender
{
    //
    [super creatingAnimationHasEnd:sender];
    agentStateType = kStatePatrol;
    //
}
//
-(void) checkEscape
{
    //
    BOOL evade = NO;
    if(sprite.position.x > ([[CoreSettings sharedCoreSettings] upperRight].x - LEAVE_EVADE_DISTANCE))
    {
        evade = YES;
    }
    //
    else if(sprite.position.x < ([[CoreSettings sharedCoreSettings] upperLeft].x + LEAVE_EVADE_DISTANCE))
    {
        evade = YES;
    }
    //
    else if(sprite.position.y > ([[CoreSettings sharedCoreSettings] upperRight].y - LEAVE_EVADE_DISTANCE))
    {
        evade = YES; 
    }
    //
    else if(sprite.position.y < ([[CoreSettings sharedCoreSettings] bottomRight].y + LEAVE_EVADE_DISTANCE))
    {
        evade = YES;
    }
    //
    if (evade) {
        escapePoint = [[SpawnManager sharedSpawnManager] getValidSpawnPosFromCharacter];
        agentStateType = kStateEvade;
    }
    //
}
//
-(void) evade
{
    //
    speed = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ?
    ULTRA_SPEED*(1 - [[[CCDirector sharedDirector] scheduler] timeScale]):
    ULTRA_SPEED;
    //
    if(ccpDistance(sprite.position, escapePoint) < LEAVE_EVADE_DISTANCE)
    {
        speed = NORMAL_SPEED;
        agentStateType = kStatePatrol;
        return;
    }
    //
    CGPoint characterPosition = escapePoint;
	CGPoint currentAgentPosition = sprite.position;
	//
	CGPoint vector = [[Utils sharedUtils] vectorBetweenTwoPoint:currentAgentPosition :characterPosition];
    vector = ccpNormalize(vector);
    faceDirection = vector;
    float rotAlfa = acosf((vector.x * 0) + (vector.y * 1));
    rotAlfa = CC_RADIANS_TO_DEGREES(rotAlfa);
    //NSLog(@"%f",rotAlfa);
    rotAlfa = (characterPosition.x - currentAgentPosition.x < 0) ? -rotAlfa : rotAlfa;
    //sprite.rotation = rotAlfa;
    vector = ccpMult(vector, ULTRA_SPEED);
    movementVelocity = vector;
    [self moveToPosition:vector];
    //
    if(ccpDistance(sprite.position, escapePoint) < LEAVE_EVADE_DISTANCE)
    {
        speed = NORMAL_SPEED;
        agentStateType = kStatePatrol;
        return;
    }
}
//
-(void) update
{
	[super update];
    if(agentStateType!=kStateEvade)
        [self checkEscape];
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
    if(fabs(sprite.rotation ) >= fabs((180 - indexer*[[AiInput sharedAiInput] mainCharacterRotation]) - 1.5*EVADE_DELTA_ALPHA) &&
       fabs(sprite.rotation ) <= fabs((180 - indexer*[[AiInput sharedAiInput] mainCharacterRotation]) + 1.5*EVADE_DELTA_ALPHA))
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
        radius = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? [Utils getRandomNumber:15 to:25]:[Utils getRandomNumber:12 to:20];
        alphaDelta = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? [Utils getRandomNumber:4 to:6] : [Utils getRandomNumber:2 to:5];
        spiralAlpha = 0;
    }
}
//
-(void) fastEvade
{
    CGPoint newVec = (perpendicularRandomer == 0) ? ccpRPerp(movementVelocity) : ccpPerp(movementVelocity);
    newVec = ccpMult(newVec, [[GameManager sharedGameManager] bulletinTimeIsOn] ? 1.5*speed : speed);
    newVec = ccp(newVec.x,-newVec.y);
    movementVelocity = ccpAdd(movementVelocity, newVec);
}
//
-(void) hit
{
    [super hit];
    float spriteOpacity = sprite.opacity;
    spriteOpacity-=opacityDecreaserStep;
    spriteOpacity = (spriteOpacity < MIN_OPACITY) ? MIN_OPACITY : spriteOpacity;
    sprite.opacity = spriteOpacity;
    //
    if(flag == kDealloc)
        [[GameManager sharedGameManager] levelHasWonAfterBigBoss];
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
    //
    if(hitCount<=0)
    {
        for (int i =0; i<vitaminsCount; i++) {
            CGPoint randomPos = [[VitaminManager sharedVitaminManager] getValidBonusSpawnPointAtPoint:sprite.position];
            [[VitaminManager sharedVitaminManager] createVitaminAtPosition:randomPos withScore:1];
        }
    }
    //
    [super releaseAgent];
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
}
//
//
@end
