//
//  Weapon.h
//  Game
//
//  Created by Giorgi Abelashvili on 10/19/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "Figure.h"
#import "Bullet.h"
#include <vector>

using namespace std;
@interface Weapon : NSObject{

	WeaponType weaponType;
	BulletType bulletType;
	float shootSpeed;
    vector <Bullet*> bullets;
    id parentObject;
}
//
@property (nonatomic,assign) float shootSpeed;
@property (nonatomic,assign) WeaponType weaponType;
@property (nonatomic,assign) BulletType bulletType;
@property (nonatomic,retain) id parentObject;
//
+(Weapon*)sharedWeapon;
//
-(Bullet*) shootAtPosition:(CGPoint)position
          andWithDirection:(CGPoint)direction
            andRotateAngle:(float)rotateAngle
      andCharacterVelocity:(CGPoint)characterVelocity;
//
-(Bullet*) shootAtPosition:(CGPoint)position
              andRotation:(float)angle
             andDirection:(CGPoint)direction
            andStartDelta:(float)startDelta;
//
-(void) updateBullets;
//
-(void) restart;
//
-(BOOL) isEmpty;
//
-(void) gameOver;
//
-(void) removeAllBulletsInstantly;
//
@end
