//
//  CharacterPropertiesManager.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 6/22/13.
//
//

#import <Foundation/Foundation.h>

@interface CharacterPropertiesManager : NSObject
{
    ShootType currentShootType;
    BOOL isDoubleSpeed;
    BOOL isImmortal;
}
//
@property (nonatomic,assign) ShootType currentShootType;
@property (nonatomic,assign) BOOL isDoubleSpeed;
@property (nonatomic,assign) BOOL isImmortal;
+(CharacterPropertiesManager*) sharedCharacterPropertiesManager;
-(void) saveProperties;
//
@end
