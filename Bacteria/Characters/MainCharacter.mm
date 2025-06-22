//
//  MainCharacter.mm
//  GeorgianTale
//
//  Created by Giorgi Abelashvili on 5/9/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//
//
#import "MainCharacter.h"
#import "SoundManager.h"
#import "Utils.h"
#import "GameResourceManager.h"
#import "KeyBind.h"
#import "InputManager.h"
#import "Weapon.h"
#import "CCSpriteBatchNodeBlur.h"
#import "AiInput.h"
#import "CCBlade.h"
#import "AgentsManager.h"
#import "LevelManager.h"
#import "Level.h"
#import "CharacterPropertiesManager.h"
//
#define STANDART_CHARACTER_VELOCITY         1.7f
#define MOUSE_MOVE_CHARACTER_VELOCITY       2.1f
#define STRAIGHT_MOVEMENT_VELOCITY_SCALE    1.3f
#define MAIN_SCALE_FACTOR                   0.12f
#define MAX_SCALE_LIMIT                     6
#define SHOOT_DIFFERENCE_ANGLE              4.0f
#define EXPLOSION_SHOOT_VARIANCE            36
#define TORNADO_DEFAULT_ALPHA_INCREASER     10.0f
#define TORNADO_DEFAULT_ALPHA_INCREASER_2   8.0f
#define BONUS_DISAPPEAR_LIMIT               7.0f
#define BONUS_TIME_COUNTER_LIMIT            8.0f
#define AUTOFIRE_START_SHOOT_TIME           2.0f
//
@interface MainCharacter (PrivateMethods)
-(void) shoot;
-(void) windmillShoot;
-(void) explosionShoot;
-(void) tornadoShoot;
-(void) reviveCHaracterAfterHit:(id)sender;
-(void) runSpecialAbilitiesTimer;
-(void) specialAbilitieActionTimer:(NSTimer*)timer;
-(void) characterBonusActionHasEnded;
-(void) startAutoFireShoot:(NSTimer*)timer;
@end
//
@implementation MainCharacter
//
@synthesize currentScale;
@synthesize shooting;
@synthesize characterRotation;
@synthesize characterDirection;
@synthesize shootCount;
@synthesize characterShootType;
@synthesize characterIsHitting;
@synthesize currentSpecialAbilitie;
@synthesize canUseSpecialAbilitie;
@synthesize canShoot;
//
//
-(id) createAtPosition:(CGPoint)position withGameLayer:(id)g
{
    //
    characterIsHitting = NO;
	gameLayer = g;
	bullets = [[NSMutableArray alloc] init];
    weaponManager = [WeaponManager sharedWeaponManager];
    currentSpecialAbilitie = kSlowMotion;
    //
    width = 1.3f;
    height = 1.3f;
    characterDirection = ccp(1.0f,0.0f);
    shootCount = 1;
    moving = NO;
    shooting = NO;
    type = kMainCharacter;
    flag = kDefault;
    life = 3;
    canUseSpecialAbilitie = YES;
    bonusLastSecondWarn = NO;
    bonusTimeCounterTick = 0.0f;
    //
	sprite = [CCSprite spriteWithSpriteFrameName:@"character.png"];
	sprite.position = position;
    characterShootType = kNormalShoot;
    //
	//
    position.x /= ASPECT_RATIO;
    position.y /= ASPECT_RATIO;
    tornadoShootAlphaIncreaser = TORNADO_DEFAULT_ALPHA_INCREASER;
    //
    [self createNewBodyAt:position andDimension:ccp(width,height)];
    //
    [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:sprite z:0 tag:1];
    //
    [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] setBlurSize:1.0f];
    //
    [self updateCharacterSettingsAfterLevel];
    //
    if([[GameSettingsManager sharedGameSettingsManager] isAutoFireEnabled]) {
        [NSTimer scheduledTimerWithTimeInterval:AUTOFIRE_START_SHOOT_TIME
                                         target:self
                                       selector:@selector(startAutoFireShoot:)
                                       userInfo:nil
                                        repeats:NO];
    }
    //
    immortal = [[CharacterPropertiesManager sharedCharacterPropertiesManager] isImmortal];
    isDoubleSpeed = [[CharacterPropertiesManager sharedCharacterPropertiesManager] isDoubleSpeed];
    characterShootType = defaultShootType = [[CharacterPropertiesManager sharedCharacterPropertiesManager] currentShootType];
    //
    return self;
}
//
//
-(void) startAutoFireShoot:(NSTimer*)timer
{
    [self startShoot];
}
//
//
- (void) createNewBodyAt:(CGPoint)position andDimension:(CGPoint)dimension
{
    //
	b2BodyDef bodyDef;
    //
    b2BodyDef bd;
    bd.position.Set(position.x, position.y);
    bd.type = b2_dynamicBody;
    bd.fixedRotation = false;
    bd.allowSleep = false;
    bd.userData = self;
	//
    body = [[Figure sharedPhysicsWorld] getPhysicsWorld]->CreateBody(&bd);
    //
    b2CircleShape shape;
    shape.m_radius = dimension.x / 2.0f;
    //
    b2FixtureDef fd;
    fd.shape = &shape;
    fd.density = 1.0f;
    fd.isSensor = false;
    body->CreateFixture(&fd);
}
//
-(void) updateCharacterPosWithNewPosition:(CGPoint)position
{
    //
	[sprite setPosition:position];
    //
    position.x /= ASPECT_RATIO;
    position.y /= ASPECT_RATIO;
    [super updatePhysicsBodyPosition:position];
    //
}
//
-(CGPoint) moveCharacterWithVelocity:(CGPoint)velocity
{
	//
    velocity = ccpMult(velocity, [Utils timeScale]);
	currentSpeed = velocity.x * velocity.y;
    body->ApplyLinearImpulse(b2Vec2(velocity.x,velocity.y), body->GetPosition());
    body->SetLinearDamping(6.0f);
    b2Vec2 position = body->GetPosition();
	sprite.position = ccp(position.x*ASPECT_RATIO,position.y*ASPECT_RATIO);
    [[AiInput sharedAiInput] setMainCharacterPosition:sprite.position];
    //
	return ccp(sprite.position.x,sprite.position.y);
    //
}
//
-(void) moveCharacterWithKeys:(int [])keys  :(int[])shootKeys
{
    //
    if(keys[0] == 1 ^ keys[1] == 1)
        velocityForMovement = ccp(velocityForMovement.x,(keys[0] == 1 ? STANDART_CHARACTER_VELOCITY : -STANDART_CHARACTER_VELOCITY));
    else 
        velocityForMovement = ccp(velocityForMovement.x,0.0f);
    //
    if(keys[2] == 1 ^ keys[3] == 1)
        velocityForMovement = ccp((keys[2] == 1 ? -STANDART_CHARACTER_VELOCITY : STANDART_CHARACTER_VELOCITY),velocityForMovement.y);
    else velocityForMovement = ccp(0.0f,velocityForMovement.y);
    //
    //
    if(velocityForMovement.x == 0&&velocityForMovement.y!=0)
        velocityForMovement = ccp(velocityForMovement.x,velocityForMovement.y*STRAIGHT_MOVEMENT_VELOCITY_SCALE);
    if(velocityForMovement.x!=0 && velocityForMovement.y == 0)
        velocityForMovement = ccp(velocityForMovement.x*STRAIGHT_MOVEMENT_VELOCITY_SCALE,velocityForMovement.y);        
    //
    //
    //keyboard shoot direction
    if(shootKeys[0] == 1 ^ shootKeys[1] == 1)
        keyBoardShootDirection = ccp(keyBoardShootDirection.x,(shootKeys[0] == 1 ? 1 : -1));
    else 
        keyBoardShootDirection = ccp(keyBoardShootDirection.x,0.0f);
    //
    if(shootKeys[2] == 1 ^ shootKeys[3] == 1)
        keyBoardShootDirection = ccp((shootKeys[2] == 1 ? -1 : 1),keyBoardShootDirection.y);
    else keyBoardShootDirection = ccp(0.0f,keyBoardShootDirection.y);
    //
    // if we rotate with keyboard
    if([InputManager sharedInputManager].characterMoveType == kOnlyKeyboard)
    {
        if(keyBoardShootDirection.x == 0 && keyBoardShootDirection.y == 1)
            characterRotation = 0.0f;
        if(keyBoardShootDirection.x == 1 && keyBoardShootDirection.y == 1)
            characterRotation = 45.0f;
        if(keyBoardShootDirection.x == 1 && keyBoardShootDirection.y == 0)
            characterRotation = 90.0f;
        if(keyBoardShootDirection.x == 1 && keyBoardShootDirection.y == -1)
            characterRotation = 135;
        if(keyBoardShootDirection.x == 0 && keyBoardShootDirection.y == -1)
            characterRotation = 180.0f;
        if(keyBoardShootDirection.x == -1 && keyBoardShootDirection.y == -1)
            characterRotation = -135.0f;
        if(keyBoardShootDirection.x == -1 && keyBoardShootDirection.y == 0)
            characterRotation = -90.0f;
        if(keyBoardShootDirection.x == -1 && keyBoardShootDirection.y == 1)
            characterRotation = -45.0f;
        //
        characterDirection = keyBoardShootDirection;
        //
    }
    if(isFastRun && isDoubleSpeed == NO)
        velocityForMovement = ccpMult(velocityForMovement, 2);
    if(isDoubleSpeed)
        velocityForMovement = ccpMult(velocityForMovement, 2);
}
//
-(void) rotateCharacterWithMousePos:(CGPoint)mousePos
{
    //
    // get current position
    CGPoint position = sprite.position;
    CGPoint vector = [[Utils sharedUtils] vectorBetweenTwoPoint:position :mousePos];
    //
    vector = ccpNormalize(vector);
    characterDirection = vector;
    float rotAlfa = acosf((vector.x * 0) + (vector.y * 1));
    rotAlfa = CC_RADIANS_TO_DEGREES(rotAlfa);
    rotAlfa = (position.x - mousePos.x < 0) ? rotAlfa : -rotAlfa;
    characterRotation = rotAlfa;
    //
}
//
-(void) moveCharacterToMousePositions:(CGPoint)mousePositions
{
    CGPoint position = sprite.position;
    CGPoint vector = [[Utils sharedUtils] vectorBetweenTwoPoint:position :mousePositions];
    //
    vector = ccpNormalize(vector);
    characterDirection = vector;
    //
    velocityForMovement = ccp(characterDirection.x*MOUSE_MOVE_CHARACTER_VELOCITY,
                              characterDirection.y*MOUSE_MOVE_CHARACTER_VELOCITY);
    //
    if(isFastRun && isDoubleSpeed == NO)
        velocityForMovement = ccpMult(velocityForMovement, 2);
    //
    if(isDoubleSpeed)
        velocityForMovement = ccpMult(velocityForMovement, 2);
    //
}
//
-(void) moveCharacterWithPosition:(CGPoint)position
{
}
//
-(void) startShoot
{
    //
    if (flag == kDealloc || canShoot == NO) {
        return;
    }
    //
    float shootRate = 0.0f;
	if([[GameManager sharedGameManager] bulletinTimeIsOn])
    {
        shootRate = (characterShootType == kWaterShoot || characterShootType == kTornadoShoot) ?
        weaponManager.currentWeapon.shootSpeed/2.4 :
        weaponManager.currentWeapon.shootSpeed*2.4f;
        //
        if(characterShootType == kExplosionShoot)
            shootRate = 0.5f;
        
        tornadoShootAlphaIncreaser = TORNADO_DEFAULT_ALPHA_INCREASER_2;
    }
    //
    else if([[GameManager sharedGameManager] bulletinTimeIsOn] == NO){
        shootRate = (characterShootType == kWaterShoot || characterShootType == kTornadoShoot) ?
        weaponManager.currentWeapon.shootSpeed/5:
        weaponManager.currentWeapon.shootSpeed;
        //
        if(characterShootType == kExplosionShoot)
            shootRate = 0.2f;
        
        tornadoShootAlphaIncreaser = TORNADO_DEFAULT_ALPHA_INCREASER;
    }
    //
	shootingTimerTimer = [NSTimer scheduledTimerWithTimeInterval:shootRate
														  target:self
														selector:@selector(shooting:)
														userInfo:nil
														 repeats:YES];
}
// Test comment
-(void) shooting:(NSTimer*)timer
{
    //
    if([[GameManager sharedGameManager] gameIsPaused] || life == 0)
        return;
    //
    if(characterDirection.x ==0 && characterDirection.y == 0)
        return;
    
    switch (characterShootType) {
        case kNormalShoot:
        case kWaterShoot:
            [self shoot];
            break;
        case kWindilShoot:
            [self windmillShoot];
            break;
        case kExplosionShoot:
            [self explosionShoot];
            break;
        case kTornadoShoot:
            [self tornadoShoot];
            break;
        default:
            break;
    }
    shooting = YES;
}
//
-(void) runSpecialAbilitiesTimer
{
    isInSpecialAbilitie = YES;
    specialAbilitiesTimerVal = [NSTimer scheduledTimerWithTimeInterval:0.1f
                                                                target:self
                                                              selector:@selector(specialAbilitieActionTimer:)
                                                              userInfo:nil
                                                               repeats:YES];
}
//
-(void) specialAbilitieActionTimer:(NSTimer*)timer
{
    //
    if([[GameManager sharedGameManager] gameIsPaused] == NO)
    {
        specialAbilitieTimeCounter+=SPECIAL_ABILITIE_TIMER_STEP;
        [[GameManager sharedGameManager] updateSpecialAbiliteProgressTimer:YES];
        if(specialAbilitieTimeCounter>=SPECIAL_POWER_TIME_LIMIT ||
           [[GameManager sharedGameManager] levelHasWon])
        {
            //
            if(specialAbilitiesTimerVal!=nil)
            {
                //
                canUseSpecialAbilitie = NO;
                //
                specialAbilitieTimeCounter = 0;
                //
                [specialAbilitiesTimerVal invalidate];
                specialAbilitiesTimerVal = nil;
                //
                if(isSlowMotion)
                {
                    isSlowMotion = NO;
                    [[GameManager sharedGameManager] goBulletinTine];
                }
                //
                isInSpecialAbilitie = NO;
                //
                [[GameManager sharedGameManager] updateSpecialAbiliteProgressTimer:NO];
                //
            }
            //
        }
        //
    }
    //
}
//
-(void) updateCharacterSettingsAfterLevel
{
    //
    if([[[LevelManager sharedLevelManager] currentLevel] number] >=11 &&
       [[[LevelManager sharedLevelManager] currentLevel] number] <=20)
    {
        characterShootType = defaultShootType;
        shootCount = 2;
    }
    if([[[LevelManager sharedLevelManager] currentLevel] number] >=21 &&
       [[[LevelManager sharedLevelManager] currentLevel] number] <=30)
    {
        characterShootType = defaultShootType;
        shootCount = 3;
    }
    if([[[LevelManager sharedLevelManager] currentLevel] number] >=31 &&
       [[[LevelManager sharedLevelManager] currentLevel] number] <=50)
    {
        characterShootType = defaultShootType;
        shootCount = 4;
    }
    // we have boss level
    if([[[LevelManager sharedLevelManager] currentLevel] number] % BOSS_LEVEL_CREATE_CONST == 0 &&
       [[[LevelManager sharedLevelManager] currentLevel] number] > 0)
        canUseSpecialAbilitie = NO;
    else canUseSpecialAbilitie = YES;
    life = 3;
    bonusTimeCounterTick = 0.0f;
    gotUsableBonus = NO;
    gotWeapon = NO;
    gotSpecialAbilitie = NO;
    characterShootType = defaultShootType;
}
//
-(void) getBonusWithType:(BonusObjectType)bonusObjectType
{
    [sprite stopAllActions];
    sprite.opacity = 255;
    sprite.visible = YES;
    currentBonusObjectType = bonusObjectType;
    NSString *bonusString = @"";
    characterIsHitting = NO;
    switch (bonusObjectType) {
        //
        case kWaterShootBonus:
            characterShootType = kWaterShoot;
            gotUsableBonus = YES;
            sprite.color = ccc3(255, 0, 255);
            gotWeapon = YES;
            gotSpecialAbilitie = NO;
            bonusString = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_WATER_SHOOT];
            break;
        case kExplosionShootBonus:
            characterShootType = kExplosionShoot;
            gotUsableBonus = YES;
            sprite.color = ccc3(255, 0, 255);
            gotWeapon = YES;
            gotSpecialAbilitie = NO;
            bonusString = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_CIRCLE_SHOOT];
            break;
        case kWindmillShootBonus:
            characterShootType = kWindilShoot;
            gotUsableBonus = YES;
            sprite.color = ccc3(255, 0, 255);
            gotWeapon = YES;
            gotSpecialAbilitie = NO;
            bonusString = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_WINDMLL_SHOOT];
            break;
        case kTornadoShootBonus:
            characterShootType = kTornadoShoot;
            gotUsableBonus = YES;
            sprite.color = ccc3(255, 0, 255);
            gotWeapon = YES;
            gotSpecialAbilitie = NO;
            bonusString = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_TORNADO_SHOOT];
            break;
        case kLifeBonusBonus:
            life = (life < 3) ? life+1 : 3;
            [[GameManager sharedGameManager] updateHeartCountOnLevelOverLayer:YES];
            bonusString = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_LIFE];
            break;
        case kMakeInvisibleBonus:
            gotUsableBonus = YES;
            gotSpecialAbilitie = YES;
            gotWeapon = NO;
            sprite.opacity = 50.0f;
            isInvisible = YES;
            bonusString = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_IMMORTALITY];
            break;
        case kExplostionBonus:
            [[AgentsManager sharedAgentsManager] bigExplosion];
            bonusString = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_BOOOM];
            break;
        case kFastMovementBonus:
            gotUsableBonus = YES;
            gotSpecialAbilitie = YES;
            gotWeapon = NO;
            sprite.color = ccc3(255, 0, 255);
            isFastRun = YES;
            bonusString = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_DOUBLE_SPEED];
            break;
        default:
            break;
    }
    //
    if(gotWeapon && shooting) {
        [self endShoot];
        [self startShoot];
    }
    //
    [[GameManager sharedGameManager] updateBonusOnLevelOverLayer:bonusString];
    //
}
//
-(void) characterBonusActionHasEnded
{
    gotUsableBonus = NO;
    BOOL returnedToDefaultWeapon = NO;
    switch (currentBonusObjectType) {
        case kWaterShootBonus:
        case kExplosionShootBonus:
        case kWindmillShootBonus:
        case kTornadoShootBonus:
            returnedToDefaultWeapon = YES;
            characterShootType = defaultShootType;
            sprite.color = ccc3(255, 255, 255);
            break;
        case kFastMovementBonus:
        case kMakeInvisibleBonus:
            //NSLog(@"bonus off");
            break;
        default:
            break;
    }
    if(returnedToDefaultWeapon && shooting) {
        [self endShoot];
        [self startShoot];
    }
    isFastRun = NO;
    isInvisible = NO;
    gotWeapon = NO;
    gotSpecialAbilitie = NO;
    bonusTimeCounterTick = 0.0f;
    bonusLastSecondWarn = NO;
}
//
-(void) levelUp:(BOOL) levelUpFlag
{
    if(levelUpFlag)
    {
        shootCount++;
        if(shootCount == 4)
            shootCount = 4;
    }
    else {
        characterShootType = defaultShootType;
        shootCount = 1;
    }
        
}
//
-(void) bigExplosion
{
    //
    if(isInSpecialAbilitie == YES)
        return;
    //
    if(!isBigExplosion) {
        [[AgentsManager sharedAgentsManager] bigExplosion];
        [self runSpecialAbilitiesTimer];
        isBigExplosion = YES;
    }
}
//
-(void) fastRun
{
    //
    if(isInSpecialAbilitie == YES)
        return;
    //
    if(!isFastRun) {
        [self runSpecialAbilitiesTimer];
        isFastRun = YES;
    }
}
//
-(void) invisible
{
    //
    if(isInSpecialAbilitie == YES)
        return;
    //
    if (!isInvisible) {
        [self runSpecialAbilitiesTimer];
        isInvisible = YES;
        sprite.opacity = 50;
    }
}
//
-(void) slowMotion
{
    //
    if(isInSpecialAbilitie == YES)
        return;
    //
    if(!isSlowMotion) {
        [self runSpecialAbilitiesTimer];
        isSlowMotion = YES;
        [[GameManager sharedGameManager] goBulletinTine];
    }
}
//
-(void) pause
{
    
}
//
-(void) resume
{
    if([[GameSettingsManager sharedGameSettingsManager] isAutoFireEnabled] == NO)
        [self endShoot];
}
//
-(void) shoot
{
    
    //
    if(shootCount == 1)
    {
        //
        CGPoint newVector = [[Utils sharedUtils] vectorWithAngle:(CC_DEGREES_TO_RADIANS(characterRotation))];
        characterDirection = ccp(newVector.x,newVector.y);
        //
        [weaponManager shootAtPosition:sprite.position
                      andWithDirection:characterDirection
                        andRotateAngle:characterRotation
                  andCharacterVelocity:ccp(body->GetLinearVelocity().x,body->GetLinearVelocity().y)];
    }
    //
    else
    {
        //
        float shootAngle = (shootCount-1)*-SHOOT_DIFFERENCE_ANGLE/2;
        //
        for(int i =0; i < shootCount;i++)
        {
            //
            CGPoint newVector = [[Utils sharedUtils] vectorWithAngle:(CC_DEGREES_TO_RADIANS(characterRotation+shootAngle))];
            characterDirection = ccp(newVector.x,newVector.y);
            //
            [weaponManager shootAtPosition:sprite.position
                          andWithDirection:characterDirection
                            andRotateAngle:characterRotation
                      andCharacterVelocity:ccp(body->GetLinearVelocity().x,body->GetLinearVelocity().y)];
            //
            shootAngle+=SHOOT_DIFFERENCE_ANGLE;
            //
        }
    }
}
//
-(void) tornadoShoot
{
    //
    //int nextShootdeltaIndex = 1;
    CGPoint newVector = CGPointZero;
    //
    newVector = [[Utils sharedUtils] vectorWithAngle:(CC_DEGREES_TO_RADIANS(tornadoShootAlpha))];
    [weaponManager shootAtPosition:sprite.position
                    andWithDirection:newVector
                        andRotateAngle:tornadoShootAlpha
                andCharacterVelocity:ccp(body->GetLinearVelocity().x,body->GetLinearVelocity().y)];
    //
    //
    newVector = [[Utils sharedUtils] vectorWithAngle:(CC_DEGREES_TO_RADIANS(tornadoShootAlpha - 10.0f))];
    [weaponManager shootAtPosition:sprite.position
                  andWithDirection:newVector
                    andRotateAngle:tornadoShootAlpha
              andCharacterVelocity:ccp(body->GetLinearVelocity().x,body->GetLinearVelocity().y)];
    //
    //
    newVector = [[Utils sharedUtils] vectorWithAngle:(CC_DEGREES_TO_RADIANS(tornadoShootAlpha + 10.0f))];
    [weaponManager shootAtPosition:sprite.position
                  andWithDirection:newVector
                    andRotateAngle:tornadoShootAlpha
              andCharacterVelocity:ccp(body->GetLinearVelocity().x,body->GetLinearVelocity().y)];
    //
    tornadoShootAlpha+=3*tornadoShootAlphaIncreaser;// + nextShootdeltaIndex*5.0f;
}
//
-(void) windmillShoot
{
    float shootAngle = -5.0f;
    //
    for(int i =0; i<4; i++)
    {
        //
        CGPoint newVector = [[Utils sharedUtils] vectorWithAngle:(CC_DEGREES_TO_RADIANS(characterRotation+shootAngle))];
        characterDirection = ccp(newVector.x,newVector.y);
        [weaponManager shootAtPosition:sprite.position
                      andWithDirection:characterDirection
                        andRotateAngle:shootAngle + characterRotation
                  andCharacterVelocity:ccp(body->GetLinearVelocity().x,body->GetLinearVelocity().y)];
        //
        shootAngle+=10.0f;
        newVector = [[Utils sharedUtils] vectorWithAngle:(CC_DEGREES_TO_RADIANS(characterRotation+shootAngle))];
        characterDirection = ccp(newVector.x,newVector.y);
        [weaponManager shootAtPosition:sprite.position
                      andWithDirection:characterDirection
                        andRotateAngle:shootAngle + characterRotation
                  andCharacterVelocity:ccp(body->GetLinearVelocity().x,body->GetLinearVelocity().y)];
        //
        shootAngle+=80.0f;
        //
    }
    //
}
//
-(void) hit
{
    if(characterIsHitting == YES || isInvisible == YES || immortal == YES)
        return;
    life--;
    [sprite runAction:[CCSequence actions:[CCBlink actionWithDuration:2.0f blinks:10],
                       [CCCallFunc actionWithTarget:self selector:@selector(reviveCHaracterAfterHit:)],nil]];
    characterIsHitting = YES;
    if(life == 0)
    {
        canUseSpecialAbilitie = NO;
        [sprite stopAllActions];
        flag = kDealloc;
        sprite.opacity = 0;
        [weaponManager gameOver];
    }
    //
    [[GameManager sharedGameManager] updateHeartCountOnLevelOverLayer:NO];
    //
}
//
-(void) reviveCHaracterAfterHit:(id)sender
{
    characterIsHitting = NO;
    sprite.opacity = 255;
}
//
-(void) refresh
{
    //
    tornadoShootAlphaIncreaser = ([[GameManager sharedGameManager] bulletinTimeIsOn]) ?
    tornadoShootAlphaIncreaser = TORNADO_DEFAULT_ALPHA_INCREASER_2 :
    tornadoShootAlphaIncreaser = TORNADO_DEFAULT_ALPHA_INCREASER;
    //
    if(shooting == NO)
        return;
    //
    [self endShoot];
    float shootRate = 0.0f;
    if([[GameManager sharedGameManager] bulletinTimeIsOn])
    {
        shootRate = (characterShootType == kWaterShoot || characterShootType == kTornadoShoot) ?
        weaponManager.currentWeapon.shootSpeed/2.4 :
        weaponManager.currentWeapon.shootSpeed*2.4f;
        //
        if(characterShootType == kExplosionShoot)
            shootRate = 0.5f;
    }
    //
    else if([[GameManager sharedGameManager] bulletinTimeIsOn] == NO){
        shootRate = (characterShootType == kWaterShoot || characterShootType == kTornadoShoot) ?
        weaponManager.currentWeapon.shootSpeed/3 :
        weaponManager.currentWeapon.shootSpeed;
        //
        if(characterShootType == kExplosionShoot)
            shootRate = 0.2f;
    }
    //
    shooting = NO;
    //
    if(canShoot == YES)
    {
        //
        if([[GameSettingsManager sharedGameSettingsManager] isAutoFireEnabled] == NO)
            [self startShoot];
        //
        else {
            shootingTimerTimer = [NSTimer scheduledTimerWithTimeInterval:shootRate
                                                                  target:self
                                                                selector:@selector(shooting:)
                                                                userInfo:nil
                                                                 repeats:YES];
            //
        }
    }
}
//
-(void) explosionShoot
{
    float shootAngle = 10.0f;
    for(int i = 0; i < EXPLOSION_SHOOT_VARIANCE; i++)
    {
        //
        CGPoint newVector = [[Utils sharedUtils] vectorWithAngle:(CC_DEGREES_TO_RADIANS(characterRotation+i*shootAngle))];
        characterDirection = ccp(newVector.x,newVector.y);
        //
        [weaponManager shootAtPosition:sprite.position
                      andWithDirection:characterDirection
                        andRotateAngle:characterRotation + i*shootAngle
                  andCharacterVelocity:ccp(body->GetLinearVelocity().x,body->GetLinearVelocity().y)];

    }
}
//
-(void) endShoot
{
	if(shootingTimerTimer)
	{
		[shootingTimerTimer invalidate];
		shootingTimerTimer = nil;
        shooting = NO;
	}
}
//
-(void) update
{
    [[AiInput sharedAiInput] setMainCharacterRotation:characterRotation];
	glowSprite.rotation = sprite.rotation = characterRotation;
    [weaponManager update];
    [self moveCharacterWithVelocity:velocityForMovement];
    [[AiInput sharedAiInput] setCharacterIsshooting:shooting];
    //
    if(gotUsableBonus) {
        //
        if(bonusTimeCounterTick >= BONUS_DISAPPEAR_LIMIT && bonusLastSecondWarn == NO) {
            bonusTimeCounterTick += GAME_NEXT_SIMULATION_STEP_INTERVAL * [Utils timeScale];
            if(gotWeapon || isFastRun)
            {
                [sprite runAction:[CCTintTo actionWithDuration:1.5f
                                                           red:255 green:255 blue:255]];
            }
            //
            else if(isInvisible)        // invisible bonus
            {
                [sprite runAction:[CCFadeIn actionWithDuration:1.0f]];
            }
            bonusLastSecondWarn = YES;
        }
        //
        if(bonusTimeCounterTick <= BONUS_TIME_COUNTER_LIMIT) {
            bonusTimeCounterTick += GAME_NEXT_SIMULATION_STEP_INTERVAL * [Utils timeScale];
        }
        //
        else {
            [self characterBonusActionHasEnded];
        }
        //
    }
}
//
//
-(void) resetCharacterBody
{
    //
    //
}
//
//
-(void) simulateCharacterRestartAtPosition:(CGPoint)position
{
    sprite.visible = YES;
    sprite.opacity = 255;
    if(specialAbilitiesTimerVal)
    {
        [specialAbilitiesTimerVal invalidate];
        specialAbilitiesTimerVal = nil;
    }
    flag = kDefault;
    float x = [[CCDirector sharedDirector] winSize].width / 2.0f;
    float y = [[CCDirector sharedDirector] winSize].height / 2.0f;
    [self endShoot];
    shooting = NO;
    sprite.position = ccp(x,y);
    x/=ASPECT_RATIO;
    y/=ASPECT_RATIO;
    body->SetTransform(b2Vec2(x,y), 0);
    sprite.position = ccp(x,y);
    [weaponManager restart];
    flag = kDefault;
    characterIsHitting = NO;
    life = 3;
    body->SetActive(true);
    //
    canUseSpecialAbilitie = YES;
    specialAbilitieTimeCounter = 0;
    isBigExplosion = NO;
    isFastRun = NO;
    if(isInvisible)
    {
        isInvisible = NO;
        sprite.opacity = 255;
    }
    //
    if(isSlowMotion)
    {
        isSlowMotion = NO;
        [[GameManager sharedGameManager] goBulletinTine];
    }
    bonusTimeCounterTick = 0.0f;
    gotUsableBonus = NO;
    gotWeapon = NO;
    characterShootType = defaultShootType;
    isInSpecialAbilitie = NO;
    gotSpecialAbilitie = NO;
    [sprite stopAllActions];
    //sprite.opacity = 255;
    sprite.visible = YES;
    sprite.color = ccc3(255, 255, 255);
    //[sprite runAction:[CCFadeIn actionWithDuration:0.01f]];
    //
    if([[GameSettingsManager sharedGameSettingsManager] isAutoFireEnabled]) {
        [self startShoot];
    }
}
//
//
-(void) releaseCharacter
{
    //
    if(specialAbilitiesTimerVal)
    {
        [specialAbilitiesTimerVal invalidate];
        specialAbilitiesTimerVal = nil;
    }
    //
    [[AiInput sharedAiInput] setCharacterIsshooting:NO];
    [[AiInput sharedAiInput] setMainCharacterPosition:ccp(0,0)];
    [[AiInput sharedAiInput] setMainCharacterRotation:0];
    [self endShoot];
    shooting = NO;
    characterIsHitting = NO;
    flag = kDefault;
    life = 3;
    [weaponManager restart];
    [[Figure sharedPhysicsWorld] getPhysicsWorld]->DestroyBody(body);
    [sprite removeFromParentAndCleanup:YES];
    bonusTimeCounterTick = 0.0f;
    gotUsableBonus = NO;
    gotWeapon = NO;
    characterShootType = defaultShootType;
    gotSpecialAbilitie = NO;
}
//
-(void) reset
{
	
}
//
@end
//
//