//
//  Inferno.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/12/13.
//
//

#import "Inferno.h"
#import "Utils.h"
#import "GameResourceManager.h"
#import "SpawnManager.h"
#import "AiInput.h"
#import "Tentacle.h"
//
#define TENTACLE_COUNT          10
#define HIT_LIMIT_1             200
#define HIT_LIMIT_2             100
#define HIT_LIMIT_3             50
#define MIN_OPACITY             70
//
int speeds[2] =
{
    10,
    16,
};
//
bool wavesAreCreated[2] =
{
    false,
    false,
};
//
@interface Tentacle (PrivateMethods)
-(void) createTentacles;
-(void) updateTentacles;
@end
//
@implementation Inferno
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
		enemyAgentType = kInferno;
		speed = 7.0f;
        tentaclesCount = TENTACLE_COUNT;
        switch ([[GameSettingsManager sharedGameSettingsManager] gameDifficulty]) {
            case kHard:
                tentaclesCount = TENTACLE_COUNT + 10;
                break;
            case kImpossible:
                tentaclesCount = TENTACLE_COUNT + 15;
                break;
            default:
                break;
        }
        //
		fleeSpeed = 5.0f;
		shootRate = 0.5f;
		shootDistance = 100;
		fleeDistance = 70.0f;
		faceDirection = ccp(0.0f,1.0f);
        hitCount = 300;
		//
		width = 8.95f;
		height = 8.95f;
        vitaminsCount = 10;
		//
		sprite = [CCSprite spriteWithSpriteFrameName:@"Enemy3.png"];
        glowSprite = [CCSprite spriteWithSpriteFrameName:@"Shine3.png"];
		//
		glowSprite.position = sprite.position = position;
		//
		position.x /= ASPECT_RATIO;
		position.y /= ASPECT_RATIO;
		//
		[super createNewBodyAt:position andDimension:ccp(width,height)];
		//
        sprite.opacity = 255;
        sprite.scale = glowSprite.scale = 5.0f;
        body->SetActive(false);
        //
		[[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:sprite z:1 tag:1];
        [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:glowSprite z:0 tag:1];
		//
        CCFadeIn *fadeIn = [CCFadeIn actionWithDuration:0.5f];
        CCCallFunc *fadeInHasDone = [CCCallFunc actionWithTarget:self selector:@selector(creatingAnimationHasEnd:)];
        [sprite runAction:[CCSequence actions:fadeIn,fadeInHasDone, nil]];
        //
        body->GetFixtureList()->SetSensor(true);
        sprite.color = ccc3(255, 255, 0);
        //
        opacityDecreaserStep = (float)sprite.opacity / (float)hitCount;
        //
        // create tentacles
        [self createTentacles];
        //
		return self;
	}
	//
	return nil;
}
//
-(void) createTentacles
{
    for (int i =0 ; i < tentaclesCount; i++) {
        CGPoint randPosition = [[SpawnManager sharedSpawnManager] getValidOuterSpawn];
        Tentacle *tentacle = [[Tentacle alloc] createAtPosition:randPosition];
        tentacle.speed = 5.0f;
        tentacles.push_back(tentacle);
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
    //
    randomMovePoint = (arc4random_uniform(2) == 1) ?
    [[AiInput sharedAiInput] mainCharacterPosition] :
    [[SpawnManager sharedSpawnManager] getRandomPointAroundPoint:ccp(randX,randY)];
}
//
-(void) creatingAnimationHasEnd:(id)sender
{
    [super creatingAnimationHasEnd:sender];
    [self generateNextRandomPoint];
    agentStateType = kStateRandomMovement;
    [sprite runAction:[CCRepeatForever actionWithAction:[CCRotateBy actionWithDuration:0.3f angle:90.0f]]];
}
//
-(void) update
{
	[super update];
    [self updateTentacles];
}
//
-(void) updateTentacles
{
    for (int i =0 ; i < (int)tentacles.size(); i++)
    {
        [tentacles[i] update];
    }
}
//
-(void) randomfly
{
    [super randomfly];
}
//
-(void) moveToPosition:(CGPoint)position
{
    [super moveToPosition:position];
    if(ccpDistance(sprite.position, randomMovePoint) < 10.0f)
        [self generateNextRandomPoint];
}
//
-(void) hit
{
    [super hit];
    if(hitCount < HIT_LIMIT_1 && hitCount > HIT_LIMIT_2)
        speed = speeds[0];
    if(hitCount < HIT_LIMIT_2 && hitCount > HIT_LIMIT_3)
        speed = speeds[1];
    //
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
-(void) releaseAgent
{
    //
    [super releaseAgent];
    //
    if(hitCount<=0)
    {
        for (int i =0; i<vitaminsCount; i++) {
            CGPoint randomPos = [[VitaminManager sharedVitaminManager] getValidBonusSpawnPointAtPoint:sprite.position];
            [[VitaminManager sharedVitaminManager] createVitaminAtPosition:randomPos withScore:1];
        }
    }
    //
    for (int i =0 ; i < (int)tentacles.size(); i++)
    {
        [tentacles[i] releaseAgent];
        [tentacles[i] release];
    }
    tentacles.clear();
}
//

//
@end
