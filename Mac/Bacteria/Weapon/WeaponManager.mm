//
//  untitled.m
//  Game
//
//  Created by Giorgi Abelashvili on 10/19/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "WeaponManager.h"
#import "Bullet.h"
//
#import "Weapon.h"
//
@implementation WeaponManager
//
@synthesize currentWeapon;
//
static WeaponManager *_sharedWeaponManager = nil;
//
+(WeaponManager*) sharedWeaponManager
{
	@synchronized([WeaponManager class])
	{
		if(!_sharedWeaponManager)
			_sharedWeaponManager = [[WeaponManager alloc] init];
	}
	return _sharedWeaponManager;
}
//
-(id) init
{
	if((self = [super init]))
	{
		currentWeapon = [[Weapon alloc] init];
		return self;
	}
	//
	return nil;
	//
}
//
-(void) shootAtPosition:(CGPoint)position
          andWithDirection:(CGPoint)direction
            andRotateAngle:(float)rotateAngle
      andCharacterVelocity:(CGPoint)characterVelocity
{
	[currentWeapon shootAtPosition:position
                  andWithDirection:direction
                    andRotateAngle:rotateAngle
              andCharacterVelocity:(CGPoint)characterVelocity];
}
//
-(void) restart
{
    [currentWeapon restart];
}
//
-(void) update
{
    [currentWeapon updateBullets];
}
//
-(void) gameOver
{
    [currentWeapon gameOver];
}
//
//
@end
