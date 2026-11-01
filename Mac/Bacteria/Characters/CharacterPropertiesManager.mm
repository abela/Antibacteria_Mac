//
//  CharacterPropertiesManager.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 6/22/13.
//
//

#import "CharacterPropertiesManager.h"
#import "NSData+AES256.h"
//
//
#define SHOOT_TYPE_KEY      @"shoottype"
#define IS_IMMORTALITY      @"immortality"
#define IS_DOUBLE_SPEED     @"doublespeed"
//
CharacterPropertiesManager *_sharedCharacterPropertiesManager = nil;
//
@implementation CharacterPropertiesManager
//
@synthesize currentShootType;
@synthesize isDoubleSpeed;
@synthesize isImmortal;
//
+(CharacterPropertiesManager*) sharedCharacterPropertiesManager
{
    @synchronized([CharacterPropertiesManager class])
    {
        if(!_sharedCharacterPropertiesManager)
            _sharedCharacterPropertiesManager = [[CharacterPropertiesManager alloc] init];
        return _sharedCharacterPropertiesManager;
    }
}
//
-(id) init
{
    if((self = [super init]))
    {
        // decript nsuserdefaults here
        NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
        
        //currentShootType = (ShootType)[[defaults objectForKey:SHOOT_TYPE_KEY] intValue];
        NSData *currentShootTypeData = [defaults objectForKey:SHOOT_TYPE_KEY];
        currentShootType = (ShootType)[[NSKeyedUnarchiver unarchiveObjectWithData:[currentShootTypeData AES256Decrypt]] intValue];
        
        //isImmortal = [[defaults objectForKey:IS_IMMORTALITY] boolValue];
        NSData *isImmortalData = [defaults objectForKey:IS_IMMORTALITY];
        isImmortal = [[NSKeyedUnarchiver unarchiveObjectWithData:[isImmortalData AES256Decrypt]] boolValue];
        
        //isDoubleSpeed =  [[defaults objectForKey:IS_DOUBLE_SPEED] boolValue];
        NSData *isDoubleSpeedData = [defaults objectForKey:IS_DOUBLE_SPEED];
        isDoubleSpeed = [[NSKeyedUnarchiver unarchiveObjectWithData:[isDoubleSpeedData AES256Decrypt]] boolValue];
        
        //
        return self;
    }
    return nil;
}
//
-(void) saveProperties
{
    // encript NSUserdefaults here!
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    
    //[defaults setObject:[NSNumber numberWithInt:currentShootType] forKey:SHOOT_TYPE_KEY];
    NSData *currentShootTypeData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithInt:currentShootType]] AES256Encrypt];
    [defaults setObject:currentShootTypeData forKey:SHOOT_TYPE_KEY];
    
    //[defaults setBool:isImmortal forKey:IS_IMMORTALITY];
    NSData *isImmortalData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithBool:isImmortal]] AES256Encrypt];
    [defaults setObject:isImmortalData forKey:IS_IMMORTALITY];
    
    //[defaults setBool:isDoubleSpeed forKey:IS_DOUBLE_SPEED];
    NSData *isDoubleSpeedData = [[NSKeyedArchiver archivedDataWithRootObject:[NSNumber numberWithBool:isDoubleSpeed]] AES256Encrypt];
    [defaults setObject:isDoubleSpeedData forKey:IS_DOUBLE_SPEED];
    
    [defaults synchronize];
}
//
@end
