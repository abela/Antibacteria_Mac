//
//  untitled.h
//  Game
//
//  Created by Giorgi Abelashvili on 10/19/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "MainGameLayer.h"
#include <vector>

@class Weapon;
@class Bullet;
@interface WeaponManager : NSObject {
	Weapon *currentWeapon;
	vector<Bullet*> bullets;
}
//
@property (nonatomic,retain) Weapon *currentWeapon;
//
+(WeaponManager*) sharedWeaponManager;
//
-(void) shootAtPosition:(CGPoint)position
          andWithDirection:(CGPoint)direction
            andRotateAngle:(float)rotateAngle
      andCharacterVelocity:(CGPoint)characterVelocity;
//
-(void) update;
-(void) restart;
-(void) gameOver;
//
@end
