//
//  MainCharacter.h
//  GeorgianTale
//
//  Created by Giorgi Abelashvili on 5/9/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "KeyBind.h"
#import "Character.h"
#import "WeaponManager.h"
#import "MainGameLayer.h"
#include <vector>
using namespace std;
//
#define SCALE_CONST         0.1f
//
@class MainGameLayer;
@interface MainCharacter : Character
{    
    //
    float currentScale;
    int scaleCounter;
    int life;
	//
	WeaponManager *weaponManager;
	//
	NSMutableArray *bullets;
	//
	BOOL shooting;
    BOOL moving;
	NSTimer *shootingTimerTimer;
	MainGameLayer *gameLayer;
	float characterRotation;
	CGPoint characterDirection;
	//
    vector <int> movementKeys;
    CGPoint velocityForMovement;
    CGPoint keyBoardShootDirection;
    CCSprite *glowSprite;
    int shootCount;
    //
    BOOL immortal;
    BOOL isDoubleSpeed;
    ShootType defaultShootType;
    //
    ShootType characterShootType;
    float tornadoShootAlpha;
    float tornadoShootAlphaIncreaser;
    BOOL characterIsHitting;
    SpecialAbilities currentSpecialAbilitie;
    //
    BOOL isInvisible;
    BOOL isFastRun;
    BOOL isBigExplosion;
    BOOL isSlowMotion;
    BOOL isInSpecialAbilitie;
    BOOL canUseSpecialAbilitie;
    float specialAbilitieTimeCounter;
    NSTimer *specialAbilitiesTimerVal;
    BOOL gotUsableBonus;
    BOOL gotWeapon;
    BonusObjectType currentBonusObjectType;
    float bonusTimeCounterTick;
    BOOL bonusLastSecondWarn;
    BOOL gotSpecialAbilitie;
    BOOL canShoot;
    //
}
//
@property (nonatomic,assign) BOOL shooting;
@property (nonatomic,assign) float currentScale;
@property (nonatomic,assign) float characterRotation;
@property (nonatomic,assign) CGPoint characterDirection;
@property (nonatomic,assign) int shootCount;
@property (nonatomic,assign) ShootType characterShootType;
@property (nonatomic,assign) BOOL characterIsHitting;
@property (nonatomic,assign) SpecialAbilities currentSpecialAbilitie;
@property (nonatomic,assign) BOOL canUseSpecialAbilitie;
@property (nonatomic,assign) BOOL canShoot;
//
-(id) createAtPosition:(CGPoint)position withGameLayer:(id)g;
//
-(void) resetCharacterBody;
//
-(void) simulateCharacterRestartAtPosition:(CGPoint)position;
//
-(void) moveCharacterWithPosition:(CGPoint)position;
//
- (void) createNewBodyAt:(CGPoint)position andDimension:(CGPoint)dimension;
//
-(void) rotateCharacterWithMousePos:(CGPoint)mousePos;
//
-(CGPoint) moveCharacterWithVelocity:(CGPoint)velocity;
//
-(void) moveCharacterToMousePositions:(CGPoint)mousePositions;
//
-(void) moveCharacterWithKeys:(int [])keys :(int[])shootKeys;
//
-(void) startShoot;
//
-(void) shooting:(NSTimer*)timer;
//
-(void) endShoot;
//
-(void) pause;
//
-(void) resume;
//
-(void) update;
//
-(void) refresh;
//
-(void) levelUp:(BOOL) levelUpFlag;
//
-(void) hit;
//
-(void) releaseCharacter;
//
-(void) bigExplosion;
//
-(void) fastRun;
//
-(void) invisible;
//
-(void) slowMotion;
//
-(void) getBonusWithType:(BonusObjectType)bonusObjectType;
//
-(void) updateCharacterSettingsAfterLevel;
//
@end
//
//
//

//
//
//
//
