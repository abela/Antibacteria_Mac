//
//  Sunux.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/4/13.
//
//

#import "Sunux.h"
#import "AiInput.h"
#import "Utils.h"
#import "GameResourceManager.h"
#import "SpawnManager.h"
#import "Weapon.h"
@implementation Sunux
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
		enemyAgentType = kSunux;
		speed = 7.0f;
		fleeSpeed = 5.0f;
		shootRate = 0.5f;
		shootDistance = 100;
		fleeDistance = 70.0f;
		faceDirection = ccp(0.0f,1.0f);
        hitCount = 20;
        shootRate = 2.0f;
        weapon = [[Weapon alloc] init];
        killScore = 150;
		//
		width = 6*0.95f;
		height = 6*0.95f;
        vitaminsCount = 7;
		//
		sprite = [CCSprite spriteWithSpriteFrameName:@"Enemy3.png"];
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
        CCFadeIn *fadeIn = [CCFadeIn actionWithDuration:0.3f];
        CCCallFunc *fadeInHasDone = [CCCallFunc actionWithTarget:self selector:@selector(creatingAnimationHasEnd:)];
        [sprite runAction:[CCRepeatForever actionWithAction:[CCRotateBy actionWithDuration:0.3f angle:90.0f]]];
        [sprite runAction:[CCSequence actions:fadeIn,fadeInHasDone, nil]];
        //
        //
        body->GetFixtureList()->SetSensor(true);
        //
        //
		return self;
	}
	//
	return nil;
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
//
-(void) refreshEnemy
{
    [self endShoot];
    shootRate = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ? 2.3f+(1-[Utils timeScale]) : 2.0f;
    [self startShoot];
}
//
-(void) shooting:(NSTimer*)timer
{
    //
    Bullet *bullet = [weapon shootAtPosition:ccp(sprite.position.x,sprite.position.y)
                                 andRotation:0.0f
                                andDirection:ccp(-1.0f,0.0f)
                               andStartDelta:100];
    //
    Bullet *bullet2 = [weapon shootAtPosition:ccp(sprite.position.x,sprite.position.y)
                                  andRotation:0.0f
                                 andDirection:ccp(-0.5f,0.5f)
                                andStartDelta:100];
    //
    Bullet *bullet3 = [weapon shootAtPosition:ccp(sprite.position.x,sprite.position.y)
                                  andRotation:0.0f
                                 andDirection:ccp(0.0f,1.0f)
                                andStartDelta:100];
    Bullet *bullet4 = [weapon shootAtPosition:ccp(sprite.position.x,sprite.position.y)
                                  andRotation:0.0f
                                 andDirection:ccp(0.5f,0.5f)
                                andStartDelta:100];
    Bullet *bullet5 = [weapon shootAtPosition:ccp(sprite.position.x,sprite.position.y)
                                  andRotation:0.0f
                                 andDirection:ccp(1.0f,0.0f)
                                andStartDelta:100];
    Bullet *bullet6 = [weapon shootAtPosition:ccp(sprite.position.x,sprite.position.y)
                                  andRotation:0.0f
                                 andDirection:ccp(0.5f,-0.5f)
                                andStartDelta:100];
    Bullet *bullet7 = [weapon shootAtPosition:ccp(sprite.position.x,sprite.position.y)
                                  andRotation:0.0f
                                 andDirection:ccp(0.0f,-1.0f)
                                andStartDelta:100];
    //
    Bullet *bullet8 = [weapon shootAtPosition:ccp(sprite.position.x,sprite.position.y)
                                  andRotation:0.0f
                                 andDirection:ccp(-0.5f,-0.5f)
                                andStartDelta:100];
    //
    bullet.tag = SHOOTING_COROSIA_BULLET_TAG;
    bullet2.tag = SHOOTING_COROSIA_BULLET_TAG;
    bullet3.tag = SHOOTING_COROSIA_BULLET_TAG;
    bullet4.tag = SHOOTING_COROSIA_BULLET_TAG;
    bullet5.tag = SHOOTING_COROSIA_BULLET_TAG;
    bullet6.tag = SHOOTING_COROSIA_BULLET_TAG;
    bullet7.tag = SHOOTING_COROSIA_BULLET_TAG;
    bullet8.tag = SHOOTING_COROSIA_BULLET_TAG;
    //
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
-(void) generateNextRandomPoint
{
    int randX = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] upperLeft].x
                                    to:[[CoreSettings sharedCoreSettings] upperRight].x];
    //
    int randY = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] bottomRight].y
                                    to:[[CoreSettings sharedCoreSettings] upperRight].y];
    randomMovePoint = [[SpawnManager sharedSpawnManager] getRandomPointAroundPoint:ccp(randX,randY)];
}
//
-(void) creatingAnimationHasEnd:(id)sender
{
    //
    [super creatingAnimationHasEnd:sender];
    [self generateNextRandomPoint];
    [self startShoot];
    //
}
//
-(void) update
{
	[super update];
    if(flag == kDying)
    {
        if([weapon isEmpty])
        {
            flag = kDealloc;
        }
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
-(void) hit
{
    hitCount -- ;
    if(hitCount <= 0)
    {
        flag = kDealloc;
        glowSprite.opacity = sprite.opacity = 0;
        [self endShoot];
        //[[VitaminManager sharedVitaminManager] createVitaminAtPosition:sprite.position withScore:1];
    }
}
//
-(void) bigExploded
{
    if(ccpDistance([[AiInput sharedAiInput] mainCharacterPosition], sprite.position) <= BIG_EXPLOSION_DISTANCE)
    {
        hitCount-=BIG_EXPLOSION_HIT_CONST;
        if(hitCount <= 0)
        {
            flag = kDying;
            glowSprite.opacity = sprite.opacity = 0;
            [self endShoot];
        }
    }
}
//
-(void) releaseAgent
{
	//
    for (int i =0; i<vitaminsCount; i++) {
        CGPoint randomPos = [[VitaminManager sharedVitaminManager] getValidBonusSpawnPointAtPoint:sprite.position];
        [[VitaminManager sharedVitaminManager] createVitaminAtPosition:randomPos withScore:1];
    }
    //
    [super releaseAgent];
    [self endShoot];
    [weapon release];
}
//

@end
