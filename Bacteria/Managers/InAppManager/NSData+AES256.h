//
//  NSData+AES256.h
//  test
//
//  Created by Jaba Odishelashvili on 6/6/13.
//  Copyright (c) 2013 Lemondo LLC. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface NSData (AES256)

- (NSData *) AES256Encrypt;
- (NSData *) AES256Decrypt;

@end
