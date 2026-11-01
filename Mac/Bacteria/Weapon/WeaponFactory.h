//
//  WeaponFactory.h
//  Game
//
//  Created by Giorgi Abelashvili on 10/19/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "Weapon.h"

@interface WeaponFactory : NSObject {

}
//
+(Weapon*) getWeaponWithType:(WeaponType)weaponType;
@end
