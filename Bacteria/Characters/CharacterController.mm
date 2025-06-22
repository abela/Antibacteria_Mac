//
//  CharacterController.m
//  Tanks
//
//  Created by Eaymon Latif on 11/22/10.
//  Copyright 2010 fvpi llc. All rights reserved.
//

#import "CharacterController.h"


@implementation CharacterController
@synthesize gameLayer,character;
//
- (id)initWithgameLayer:(MainGameLayer*)g
{
    throw [NSException exceptionWithName:@"Ovveriding!" reason:@"Must Oveeride!" userInfo:nil];
    return nil;
}
//
- (void) createCharacter
{
    throw [NSException exceptionWithName:@"Ovveriding!" reason:@"Must Oveeride!" userInfo:nil];
}
//
- (void) updateCharacterWith:(ObjectMovingFlag)direction
{
    throw [NSException exceptionWithName:@"Ovveriding!" reason:@"Must Oveeride!" userInfo:nil];
}
//
- (ObjectMovingFlag) updatePosition 
{
    throw [NSException exceptionWithName:@"Ovveriding!" reason:@"Must Oveeride!" userInfo:nil];
    return kDontMove;
}
//
- (int) shoot
{
    throw [NSException exceptionWithName:@"Ovveriding!" reason:@"Must Oveeride!" userInfo:nil];
    return nil;
}
//
-(void)resetCharacter
{
    throw [NSException exceptionWithName:@"Ovveriding!" reason:@"Must Oveeride!" userInfo:nil];
}
//
-(void) Die 
{
    throw [NSException exceptionWithName:@"Ovveriding!" reason:@"Must Oveeride!" userInfo:nil];
}
@end
